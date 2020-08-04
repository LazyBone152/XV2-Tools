using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AudioCueEditor.Audio
{
    public class AudioPlayer : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        
        private WaveOut wavePlayer = new WaveOut();
        private WavStream CurrentWav = null;

        public bool HasWave { get { return CurrentWav != null; } }

        #region PlaybackTimer
        private DispatcherTimer callbackTimer = new DispatcherTimer();

        public string PlaybackTime
        {
            get
            {
                if (CurrentWav == null) return "--/--";
                TimeSpan current = new TimeSpan(0, 0, 0, (int)CurrentWav.waveStream.CurrentTime.TotalSeconds);
                TimeSpan total = new TimeSpan(0, 0, 0, (int)CurrentWav.waveStream.TotalTime.TotalSeconds);
                return string.Format("{0}/{1}", current, total);
            }
        }

        private void CallbackTimer_Tick(object sender, EventArgs e)
        {
            //In here we need to update the playback timer, which will be done each second
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            NotifyPropertyChanged("PlaybackTime");
        }
        #endregion

        public AudioPlayer()
        {
            wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;

            //Init DispatcherTimer
            callbackTimer.Tick += CallbackTimer_Tick;
            callbackTimer.Interval = new TimeSpan(0, 0, 0, 0, 800);
            callbackTimer.Start();
        }
        
        private void WavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if(CurrentWav != null)
                CurrentWav.Dispose();
            CurrentWav = null;
        }

        //Set Audio
        public void SetAudio(WavStream wav)
        {
            if(wavePlayer.PlaybackState != PlaybackState.Stopped)
                wavePlayer.Stop();

            if (CurrentWav != null)
                CurrentWav.Dispose();
            CurrentWav = wav;

            //Load wav
            wavePlayer.Init(wav.waveStream);

            wavePlayer.Volume = 1f;
        }

        public async Task AsyncSetHcaAudio(byte[] hcaBytes)
        {
            WavStream wav = null;
            await Task.Run(() => wav = HCA.Decode(hcaBytes));
            
            if (wavePlayer.PlaybackState != PlaybackState.Stopped)
                wavePlayer.Stop();

            if (CurrentWav != null)
                CurrentWav.Dispose();
            CurrentWav = wav;
            
            //Load wav
            await Task.Run(() => wavePlayer.Init(wav.waveStream));

            wavePlayer.Volume = 1f;
        }

        /// <summary>
        /// Call this AFTER the SetAudio methods.
        /// </summary>
        /// <param name="vol">The volume, on a scale of 0 - 1.</param>
        public void SetVolume(float vol)
        {
            wavePlayer.Volume = vol;
        }

        //Helpers
        public bool HasAudio(WavStream _wavStream)
        {
            return CurrentWav == _wavStream;
        }

        //Commands
        public void Play()
        {
            if (CurrentWav == null) return;

            if(wavePlayer.PlaybackState == PlaybackState.Paused)
                wavePlayer.Resume();
            else
                wavePlayer.Play();
            
        }

        public void Stop()
        {
            if (CurrentWav == null) return;

            if(wavePlayer.PlaybackState == PlaybackState.Playing)
                wavePlayer.Stop();

            if(CurrentWav != null)
            {
                CurrentWav.Dispose();
                CurrentWav = null;
            }
        }

        public void Pause()
        {
            if (CurrentWav == null) return;
            wavePlayer.Pause();
        }

        public void Rewind()
        {
            if (CurrentWav == null) return;
            CurrentWav.waveStream.Skip(-1);
            UpdateTimer();
        }

        public void FastForward()
        {
            if (CurrentWav == null) return;
            CurrentWav.waveStream.Skip(1);
            UpdateTimer();
        }

        public void Seek(float seconds)
        {
            //CurrentWav.waveStream.Position = 0;
            //CurrentWav.waveStream.Skip((int)seconds);
            CurrentWav.waveStream.CurrentTime = new TimeSpan(0, 0, 0, (int)seconds);
        }
    }
}
