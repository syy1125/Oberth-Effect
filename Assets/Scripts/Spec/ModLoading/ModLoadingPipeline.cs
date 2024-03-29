﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Yaml;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.ModLoading
{
internal class InvalidSpecStructureException : Exception
{}

internal interface IModLoadingPipeline
{
	void ResetData();
	void LoadModContent(ModListElement mod);
	void ParseSpecInstances(Action<string, int, int> onProgress);
	void ValidateResults();
}

public class ModLoadingPipeline<TSpec> : IModLoadingPipeline
{
	private struct GameSpecDocument
	{
		public YamlDocument SpecDocument;
		public List<string> OverrideOrder;
	}

	// Configuration
	private string _idField;
	private Action<string, YamlDocument> _resolvePath;

	private string _name;
	private string _contentDirectory;

	// State
	private Dictionary<string, GameSpecDocument> _documents;
	public IReadOnlyList<SpecInstance<TSpec>> Results;

	public ModLoadingPipeline(string name, string contentDirectory)
	{
		ResolveIdField();
		SetupResolvePath();

		_name = name;
		_contentDirectory = contentDirectory;
		_documents = new();
	}

	public ModLoadingPipeline(string contentDirectory) :
		this(contentDirectory, contentDirectory)
	{}

	private void ResolveIdField()
	{
		FieldInfo[] fields = typeof(TSpec).GetFields(BindingFlags.Public | BindingFlags.Instance);

		foreach (FieldInfo field in fields)
		{
			if (field.GetCustomAttribute<IdFieldAttribute>() != null)
			{
				_idField = field.Name;
				break;
			}
		}

		if (_idField == null)
		{
			Debug.LogError($"{typeof(TSpec)} has no ID field");
		}
	}

	private void SetupResolvePath()
	{
		if (typeof(TSpec).GetCustomAttribute<ContainsPathAttribute>() == null) return;

		List<List<string>> pathFields = new();
		GetPathFields(typeof(TSpec), new(), pathFields);

		_resolvePath = (filePath, document) =>
		{
			foreach (List<string> field in pathFields)
			{
				TryResolvePath(filePath, document, field);
			}
		};
	}

	private static void GetPathFields(Type type, LinkedList<string> currentField, List<List<string>> pathFields)
	{
		FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

		foreach (FieldInfo field in fields)
		{
			if (field.GetCustomAttribute<ResolveAbsolutePathAttribute>() != null)
			{
				pathFields.Add(new(currentField) { field.Name });
			}
			else if (field.FieldType.GetCustomAttribute<ContainsPathAttribute>() != null)
			{
				currentField.AddLast(field.Name);
				GetPathFields(field.FieldType, currentField, pathFields);
				currentField.RemoveLast();
			}
		}
	}

	private static bool TryResolvePath(string filePath, YamlDocument document, List<string> field)
	{
		YamlNode node = document.RootNode;

		foreach (string step in field)
		{
			if (node is not YamlMappingNode mappingNode)
			{
				return false;
			}

			if (!mappingNode.Children.TryGetValue(step, out node))
			{
				return false;
			}
		}

		if (node is not YamlScalarNode scalarNode)
		{
			return false;
		}

		scalarNode.Value = Path.Combine(
			Path.GetDirectoryName(filePath) ?? throw new ArgumentException(),
			scalarNode.Value
		);
		return true;
	}

	public void ResetData()
	{
		_documents.Clear();
	}

	public void InjectFileContent(string content, string modName)
	{
		var reader = new StringReader(content);

		try
		{
			LoadFileDocuments(
				reader,
				new() { Spec = new() { DisplayName = modName, AllowDuplicateDefs = false } },
				null
			);
		}
		catch (YamlException)
		{
			Debug.LogError("YamlException when reading injected file!");
		}
		finally
		{
			reader.Dispose();
		}
	}

	public void LoadModContent(ModListElement mod)
	{
		string contentRoot = Path.Combine(mod.FullPath, _contentDirectory);
		if (!Directory.Exists(contentRoot)) return;

		string[] files = Directory.EnumerateFiles(contentRoot, "*.yaml", SearchOption.AllDirectories).ToArray();

		foreach (string filePath in files)
		{
			StreamReader reader = File.OpenText(filePath);

			// Each file should have strong exception guarantee
			// Either all documents in the file load and merge successfully, or none of them do.

			try
			{
				LoadFileDocuments(reader, mod, filePath);
			}
			catch (YamlException)
			{
				Debug.LogError($"Failed to parse yaml from {filePath}, skipping. This is most likely a mod problem.");
			}
			catch (InvalidSpecStructureException)
			{
				Debug.LogError($"Invalid spec structure in {filePath}, skipping. This is most likely a mod problem.");
			}
			catch (Exception)
			{
				Debug.Log(
					$"Unexpected error when loading file {filePath}. This is a problem with mod loading system!"
				);
			}
			finally
			{
				reader.Dispose();
			}
		}
	}

	private void LoadFileDocuments(TextReader reader, ModListElement mod, string filePath)
	{
		YamlStream yaml = new YamlStream();
		yaml.Load(reader);

		var altered = new Dictionary<string, YamlDocument>();

		foreach (YamlDocument document in yaml.Documents)
		{
			_resolvePath?.Invoke(filePath, document);

			try
			{
				string id = ((YamlScalarNode) document.RootNode[_idField]).Value;

				if (altered.TryGetValue(id, out YamlDocument current))
				{
					if (!mod.Spec.AllowDuplicateDefs)
					{
						Debug.LogError(
							string.Join(
								"\n",
								$"Mod {mod.Spec.DisplayName} contains multiple definitions for {_name} \"{id}\".",
								"This is most likely an issue with the mod, but we will attempt to merge the definitions together anyway.",
								$"To disable this warning, set \"{nameof(ModSpec.AllowDuplicateDefs)}\" to true in mod.json."
							)
						);
					}

					altered[id] = YamlMergeHelper.DeepMerge(current, document);
				}
				else if (_documents.TryGetValue(id, out GameSpecDocument original))
				{
					altered.Add(id, YamlMergeHelper.DeepMerge(original.SpecDocument, document));
				}
				else
				{
					altered.Add(id, YamlMergeHelper.DeepCopy(document));
				}
			}
			catch (InvalidCastException)
			{
				throw new InvalidSpecStructureException();
			}
			catch (YamlMergeException)
			{
				throw new InvalidSpecStructureException();
			}
		}

		// At this point, merge attempts are all successful, and we shouldn't see any more exceptions popping up.
		// (We can't really do anything anyway if dictionary operations and assignment operations are failing)
		foreach (KeyValuePair<string, YamlDocument> entry in altered)
		{
			if (_documents.TryGetValue(entry.Key, out GameSpecDocument document))
			{
				document.SpecDocument = entry.Value;
				document.OverrideOrder.Add(mod.Spec.DisplayName);
				_documents[entry.Key] = document;
			}
			else
			{
				_documents.Add(
					entry.Key, new GameSpecDocument
					{
						SpecDocument = entry.Value,
						OverrideOrder = new List<string>(new[] { mod.Spec.DisplayName })
					}
				);
			}
		}
	}

	public void ParseSpecInstances(Action<string, int, int> onProgress)
	{
		List<SpecInstance<TSpec>> instances = new List<SpecInstance<TSpec>>();

		var documentList = _documents.Values.ToList();

		for (var i = 0; i < documentList.Count; i++)
		{
			GameSpecDocument document = documentList[i];

			onProgress?.Invoke(_name, i, documentList.Count);

			try
			{
				var spec = ModLoader.Deserializer.Deserialize<TSpec>(
					new YamlStreamParserAdapter(document.SpecDocument.RootNode)
				);
				instances.Add(
					new()
					{
						Spec = spec,
						OverrideOrder = document.OverrideOrder
					}
				);
			}
			catch (YamlException e)
			{
				string id = ((YamlScalarNode) document.SpecDocument.RootNode[_idField]).Value;
				Debug.LogError(
					$"Deserialization error {e} when deserializing document with id \"{id}\""
				);
				Debug.Log(ModLoader.Serializer.Serialize(document.SpecDocument));
			}
		}

		Results = instances;
	}

	public void ValidateResults()
	{
		FieldInfo idField = typeof(TSpec).GetField(_idField, BindingFlags.Public | BindingFlags.Instance);
		Debug.Assert(idField != null, nameof(idField) + " != null");

		foreach (SpecInstance<TSpec> instance in Results)
		{
			List<string> errors = ValidationHelper.ValidateRootObject(instance.Spec);

			if (errors.Count > 0)
			{
				StringBuilder message = new StringBuilder()
					.AppendLine(
						$"{_name} {idField.GetValue(instance.Spec)} failed validation with {errors.Count} errors:"
					);

				foreach (string error in errors)
				{
					message.Append("  ").AppendLine(error);
				}

				Debug.LogWarning(message);
			}
		}
	}

	public IEnumerable<T> GetResultIds<T>(Predicate<TSpec> where = null)
	{
		FieldInfo idField = typeof(TSpec).GetField(_idField, BindingFlags.Public | BindingFlags.Instance);
		Debug.Assert(idField != null, nameof(idField) + " != null");

		return where == null
			? Results.Select(instance => (T) idField.GetValue(instance.Spec))
			: Results.Where(instance => @where(instance.Spec)).Select(instance => (T) idField.GetValue(instance.Spec));
	}
}
}