namespace Syy1125.OberthEffect.Foundation
{
public interface IBlockSoundAttenuator
{
	float AttenuatePersistentSound(string soundId, float volume);
	float AttenuateOneShotSound(string soundId, float volume);
}
}