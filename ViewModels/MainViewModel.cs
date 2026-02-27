using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MP3Pro.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private AudioFileReader _audioFile;
        private WaveOutEvent _outputDevice = new WaveOutEvent();
        private DispatcherTimer _playTimer;
        private double _anchorPointMs;

        [ObservableProperty] private string _selectedFilePath;
        [ObservableProperty] private string _statusMessage = "Düzenlemeye başlamak için bir dosya seçin.";
        [ObservableProperty] private double _startTime;
        [ObservableProperty] private double _endTime;
        [ObservableProperty] private string _startTimeDisplay = "00:00.000";
        [ObservableProperty] private string _endTimeDisplay = "00:00.000";
        [ObservableProperty] private float[] _waveformPeaks;
        [ObservableProperty] private double _selectionStartPos;
        [ObservableProperty] private double _selectionWidth;

        public MainViewModel()
        {
            _playTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _playTimer.Tick += (s, e) => {
                if (_audioFile != null && _audioFile.CurrentTime.TotalMilliseconds >= EndTime) Stop();
            };
        }

        public void StartSelection(double x, double totalWidth)
        {
            if (_audioFile == null) return;
            _anchorPointMs = (x / totalWidth) * _audioFile.TotalTime.TotalMilliseconds;
            UpdateSelection(x, totalWidth);
        }

        public void UpdateSelection(double x, double totalWidth)
        {
            if (_audioFile == null) return;
            double currentMs = (x / totalWidth) * _audioFile.TotalTime.TotalMilliseconds;

            StartTime = Math.Min(_anchorPointMs, currentMs);
            EndTime = Math.Max(_anchorPointMs, currentMs);

            SelectionStartPos = (StartTime / _audioFile.TotalTime.TotalMilliseconds) * totalWidth;
            double endPos = (EndTime / _audioFile.TotalTime.TotalMilliseconds) * totalWidth;
            SelectionWidth = Math.Max(2, endPos - SelectionStartPos);

            UpdateDisplays();
        }

        private void UpdateDisplays()
        {
            StartTimeDisplay = TimeSpan.FromMilliseconds(StartTime).ToString(@"mm\:ss\.fff");
            EndTimeDisplay = TimeSpan.FromMilliseconds(EndTime).ToString(@"mm\:ss\.fff");
        }

        [RelayCommand]
        private async Task OpenFileAsync()
        {
            var dialog = new OpenFileDialog { Filter = "MP3 Files|*.mp3" };
            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                StatusMessage = "Ses dosyası analiz ediliyor...";
                await Task.Run(() => {
                    _audioFile = new AudioFileReader(SelectedFilePath);
                    GenerateWaveform(SelectedFilePath);
                });
                StartTime = 0;
                EndTime = _audioFile.TotalTime.TotalMilliseconds;
                SelectionStartPos = 0;
                UpdateDisplays();
                StatusMessage = "Dosya yüklendi. Alan seçebilirsiniz.";
            }
        }

        private void GenerateWaveform(string path)
        {
            using (var reader = new AudioFileReader(path))
            {
                int samples = 800;
                float[] peaks = new float[samples];
                int frameSize = (int)(reader.Length / (samples * 4));
                float[] buffer = new float[Math.Max(frameSize, 1)];
                for (int i = 0; i < samples; i++)
                {
                    int read = reader.Read(buffer, 0, buffer.Length);
                    peaks[i] = read > 0 ? buffer.Take(read).Max(Math.Abs) : 0;
                }
                WaveformPeaks = peaks;
            }
        }

        [RelayCommand]
        private void Play()
        {
            if (_audioFile == null) return;
            _outputDevice.Stop();
            _audioFile.CurrentTime = TimeSpan.FromMilliseconds(StartTime);
            _outputDevice.Init(_audioFile);
            _outputDevice.Play();
            _playTimer.Start();
            StatusMessage = "Seçili alan oynatılıyor...";
        }

        [RelayCommand]
        private void Stop()
        {
            _outputDevice.Stop();
            _playTimer.Stop();
            StatusMessage = "Duraklatıldı.";
        }

        [RelayCommand]
        private async Task ExportAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) return;

            var dialog = new SaveFileDialog { Filter = "MP3|*.mp3", FileName = "Modern_Cut.mp3" };
            if (dialog.ShowDialog() == true)
            {
                StatusMessage = "Dışa aktarılıyor, lütfen bekleyin...";
                await Task.Run(() => {
                    using (var reader = new Mp3FileReader(SelectedFilePath))
                    using (var writer = File.Create(dialog.FileName))
                    {
                        reader.CurrentTime = TimeSpan.FromMilliseconds(StartTime);
                        while (reader.CurrentTime < TimeSpan.FromMilliseconds(EndTime))
                        {
                            var frame = reader.ReadNextFrame();
                            if (frame == null) break;
                            writer.Write(frame.RawData, 0, frame.RawData.Length);
                        }
                    }
                });
                StatusMessage = "İşlem başarıyla tamamlandı!";
                MessageBox.Show("Yeni ses dosyası başarıyla kaydedildi.", "MP3Pro", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}