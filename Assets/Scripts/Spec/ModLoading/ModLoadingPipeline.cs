using System;
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
	void LoadModContent(ModListElement mod);
	void ParseSpecInstances(IDeserializer deserializer, Action<string, int, int> onProgress);
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
	private string _name;
	private string _modsRoot;
	private string _contentDirectory;
	private Action<string, YamlDocument> _preprocess;

	// State
	private Dictionary<string, GameSpecDocument> _documents;
	public IReadOnlyList<SpecInstance<TSpec>> Results;

	public ModLoadingPipeline(
		string name, string modsRoot, string contentDirectory, Action<string, YamlDocument> preprocess
	)
	{
		ResolveIdField();
		_name = name;
		_modsRoot = modsRoot;
		_contentDirectory = contentDirectory;
		_preprocess = preprocess;
		_documents = new Dictionary<string, GameSpecDocument>();
	}

	public ModLoadingPipeline(
		string modsRoot, string contentDirectory, Action<string, YamlDocument> preprocess = null
	) :
		this(contentDirectory, modsRoot, contentDirectory, preprocess)
	{}

	private void ResolveIdField()
	{
		FieldInfo[] fields = typeof(TSpec).GetFields(BindingFlags.Public | BindingFlags.Instance);

		foreach (FieldInfo field in fields)
		{
			foreach (CustomAttributeData attributeData in field.GetCustomAttributesData())
			{
				if (attributeData.AttributeType == typeof(IdFieldAttribute))
				{
					if (_idField == null)
					{
						_idField = field.Name;
						break;
					}
					else
					{
						Debug.LogError($"{typeof(TSpec)} has multiple fields marked as ID field");
					}
				}
			}
		}

		if (_idField == null)
		{
			Debug.LogError($"{typeof(TSpec)} has no ID field");
		}
	}

	public void LoadModContent(ModListElement mod)
	{
		string contentRoot = Path.Combine(_modsRoot, mod.Directory, _contentDirectory);
		if (!Directory.Exists(contentRoot)) return;

		string[] files = Directory.EnumerateFiles(contentRoot, "*.yaml", SearchOption.AllDirectories).ToArray();

		foreach (string file in files)
		{
			StreamReader reader = File.OpenText(file);

			// Each file should have strong exception guarantee
			// Either all documents in the file load and merge successfully, or none of them do.

			try
			{
				YamlStream yaml = new YamlStream();
				yaml.Load(reader);

				var altered = new Dictionary<string, YamlDocument>();

				foreach (YamlDocument document in yaml.Documents)
				{
					_preprocess?.Invoke(file, document);

					try
					{
						string id = ((YamlScalarNode) document.RootNode[_idField]).Value;

						if (altered.TryGetValue(id, out YamlDocument current))
						{
							if (!mod.Mod.AllowDuplicateDefs)
							{
								Debug.LogError(
									string.Join(
										"\n",
										$"Mod {mod.Mod.DisplayName} contains multiple definitions for {_name} \"{id}\".",
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
					if (_documents.TryGetValue(entry.Key, out GameSpecDocument original))
					{
						original.SpecDocument = entry.Value;
						original.OverrideOrder.Add(mod.Mod.DisplayName);
					}
					else
					{
						_documents.Add(
							entry.Key, new GameSpecDocument
							{
								SpecDocument = entry.Value,
								OverrideOrder = new List<string>(new[] { mod.Mod.DisplayName })
							}
						);
					}
				}
			}
			catch (YamlException)
			{
				Debug.LogError($"Failed to parse yaml from {file}, skipping. This is most likely a mod problem.");
			}
			catch (InvalidSpecStructureException)
			{
				Debug.LogError($"Invalid spec structure in {file}, skipping. This is most likely a mod problem.");
			}
			catch (Exception)
			{
				Debug.Log(
					$"Unexpected error when loading file {file}. This is a problem with mod loading system!"
				);
			}
			finally
			{
				reader.Dispose();
			}
		}
	}

	public void ParseSpecInstances(IDeserializer deserializer, Action<string, int, int> onProgress)
	{
		List<SpecInstance<TSpec>> instances = new List<SpecInstance<TSpec>>();

		var documentList = _documents.Values.ToList();

		for (var i = 0; i < documentList.Count; i++)
		{
			GameSpecDocument document = documentList[i];

			onProgress?.Invoke(_name, i, documentList.Count);

			try
			{
				var spec = deserializer.Deserialize<TSpec>(new YamlStreamParserAdapter(document.SpecDocument.RootNode));
				instances.Add(
					new SpecInstance<TSpec>
					{
						Spec = spec,
						OverrideOrder = document.OverrideOrder
					}
				);
			}
			catch (YamlException e)
			{
				Debug.LogError(
					$"Deserialization error {e.Message} when deserializing document\n{document.SpecDocument}"
				);
				var serializer = new SerializerBuilder().Build();
				Debug.Log(serializer.Serialize(document.SpecDocument));
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

	public IEnumerable<T> GetResultIds<T>(Predicate<TSpec> @where = null)
	{
		FieldInfo idField = typeof(TSpec).GetField(_idField, BindingFlags.Public | BindingFlags.Instance);
		Debug.Assert(idField != null, nameof(idField) + " != null");

		return @where == null
			? Results.Select(instance => (T) idField.GetValue(instance.Spec))
			: Results.Where(instance => @where(instance.Spec)).Select(instance => (T) idField.GetValue(instance.Spec));
	}
}
}