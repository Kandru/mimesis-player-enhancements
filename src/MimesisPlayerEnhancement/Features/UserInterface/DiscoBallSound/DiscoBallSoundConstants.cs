namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundConstants
    {
        internal const string Feature = "Ui";
        internal const string AssetFolder = "DiscoBallSound";
        internal const string SourceObjectName = "MimesisPlayerEnhancement_DiscoBallSound";

        internal const float SpatialBlend = 1f;
        internal const float MinDistance = 4f;
        internal const float MaxDistance = 30f;

        // MasterAudio applies group/bus/master attenuation; our AudioSource does not.
        internal const float PlaybackGain = 0.45f;
    }
}
