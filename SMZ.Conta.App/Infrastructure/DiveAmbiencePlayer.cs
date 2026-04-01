using System.IO;
using System.Media;
using System.Windows;

namespace SMZ.Conta.App.Infrastructure;

internal sealed class DiveAmbiencePlayer : IDisposable
{
    private static readonly Uri DiveAudioUri = new("pack://application:,,,/Assets/suono_imm.wav", UriKind.Absolute);

    private MemoryStream? _waveStream;
    private SoundPlayer? _player;

    public void Start()
    {
        if (_player is not null)
        {
            return;
        }

        var resourceInfo = Application.GetResourceStream(DiveAudioUri);
        if (resourceInfo?.Stream is null)
        {
            return;
        }

        _waveStream = new MemoryStream();
        resourceInfo.Stream.CopyTo(_waveStream);
        _waveStream.Position = 0;

        _player = new SoundPlayer(_waveStream);
        _player.Load();
        _player.PlayLooping();
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;

        _waveStream?.Dispose();
        _waveStream = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
