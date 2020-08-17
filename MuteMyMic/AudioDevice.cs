using NAudio.CoreAudioApi;
using System.Windows.Controls;

namespace MuteMyMic
{
    public class AudioDevice
    {
        private readonly MMDevice audioDevice;
        private readonly Label muteLabel;
        private bool wasMuted;
        private float previousVolume;

        public bool IsMuted { get => audioDevice.AudioEndpointVolume.Mute; }
        public string DeviceId { get => audioDevice.ID; }

        public AudioDevice(MMDevice audioDevice, Label muteLabel)
        {
            this.audioDevice = audioDevice;
            this.muteLabel = muteLabel;
            wasMuted = this.audioDevice.AudioEndpointVolume.Mute;
            previousVolume = this.audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
        }

        public void Mute()
        {
            audioDevice.AudioEndpointVolume.Mute = true;
            audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 0;
            muteLabel.Content = "Muted";
        }

        public void Unmute()
        {
            audioDevice.AudioEndpointVolume.Mute = false;
            audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = previousVolume;
            muteLabel.Content = "Unmuted";
        }

        public void RestoreToPreviousState()
        {
            audioDevice.AudioEndpointVolume.Mute = wasMuted;
            audioDevice.AudioEndpointVolume.MasterVolumeLevelScalar = previousVolume;
        }
    }
}
