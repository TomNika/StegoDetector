using Microsoft.Win32;
using OxyPlot;
using StegoAnalysis.Models.Algorithms;
using StegoAnalysis.Models.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace StegoAnalysis.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ImageService _imageService = new();

        private string? _imagePath;
        private BitmapImage? _loadedImage;
        private bool _isHistogramVisible;
        private bool _showAnalysisButton = true;
        private bool _showVerdict;
        private string _verdictText = string.Empty;
        private string _verdictJustification = string.Empty;
        private string _verdictColor = "Gray";
        private ObservableCollection<AnalysisResultItem> _analysisResults = new();
        private PlotModel? _histogramModel;

        public string? ImagePath
        {
            get => _imagePath;
            private set
            {
                if (SetProperty(ref _imagePath, value))
                {
                    OnPropertyChanged(nameof(ImagePlaceholderVisibility));
                    OnPropertyChanged(nameof(LoadedImageVisibility));
                }
            }
        }

        public BitmapImage? LoadedImage
        {
            get => _loadedImage;
            private set => SetProperty(ref _loadedImage, value);
        }

        public bool IsHistogramVisible
        {
            get => _isHistogramVisible;
            set { if (SetProperty(ref _isHistogramVisible, value)) OnPropertyChanged(nameof(HistogramVisibility)); }
        }

        public bool ShowAnalysisButton
        {
            get => _showAnalysisButton;
            set { if (SetProperty(ref _showAnalysisButton, value)) OnPropertyChanged(nameof(AnalysisButtonVisibility)); }
        }

        public bool ShowVerdict
        {
            get => _showVerdict;
            set { if (SetProperty(ref _showVerdict, value)) OnPropertyChanged(nameof(VerdictVisibility)); }
        }

        public string VerdictText { get => _verdictText; set => SetProperty(ref _verdictText, value); }
        public string VerdictJustification { get => _verdictJustification; set => SetProperty(ref _verdictJustification, value); }
        public string VerdictColor { get => _verdictColor; set => SetProperty(ref _verdictColor, value); }
        public ObservableCollection<AnalysisResultItem> AnalysisResults { get => _analysisResults; set => SetProperty(ref _analysisResults, value); }
        public PlotModel? HistogramModel { get => _histogramModel; set => SetProperty(ref _histogramModel, value); }

        public Visibility ImagePlaceholderVisibility => string.IsNullOrEmpty(ImagePath) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LoadedImageVisibility => !string.IsNullOrEmpty(ImagePath) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HistogramVisibility => IsHistogramVisible ? Visibility.Visible : Visibility.Collapsed;
        public Visibility AnalysisButtonVisibility => ShowAnalysisButton ? Visibility.Visible : Visibility.Collapsed;
        public Visibility VerdictVisibility => ShowVerdict ? Visibility.Visible : Visibility.Collapsed;

        // Команды
        public ICommand OpenImageCommand { get; }
        public ICommand SaveReportCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand RunAnalysisCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand ChiSquareCommand { get; }
        public ICommand RsCommand { get; }
        public ICommand DctCommand { get; }
        public ICommand WaveletCommand { get; }

        public MainWindowViewModel()
        {
            OpenImageCommand = new RelayCommand(_ => OpenImage());
            SaveReportCommand = new RelayCommand(_ => SaveReport(), _ => !string.IsNullOrEmpty(ImagePath));
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            RunAnalysisCommand = new RelayCommand(_ => RunAnalysis(), _ => !string.IsNullOrEmpty(ImagePath));
            AboutCommand = new RelayCommand(_ => ShowAbout());

            ChiSquareCommand = new RelayCommand(_ => RunChiSquareOnly(), _ => !string.IsNullOrEmpty(ImagePath));
            RsCommand = new RelayCommand(_ => RunRsOnly(), _ => !string.IsNullOrEmpty(ImagePath));
            DctCommand = new RelayCommand(_ => RunDctOnly(), _ => !string.IsNullOrEmpty(ImagePath));
            WaveletCommand = new RelayCommand(_ => RunWaveletOnly(), _ => !string.IsNullOrEmpty(ImagePath));
        }

        private void OpenImage()
        {
            var path = _imageService.OpenImageFileDialog();
            if (!string.IsNullOrEmpty(path))
                SetImage(path);
        }

        public void LoadImageFromPath(string path)
        {
            if (File.Exists(path))
                SetImage(path);
        }

        private void SetImage(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            if (bitmap.CanFreeze) bitmap.Freeze();

            ImagePath = path;
            LoadedImage = bitmap;

            ShowAnalysisButton = true;
            ShowVerdict = false;
            IsHistogramVisible = false;
            AnalysisResults.Clear();
            HistogramModel = null;
            VerdictText = string.Empty;
            VerdictJustification = string.Empty;
            VerdictColor = "Gray";
        }

        private void AddResult(string method, string value) => AnalysisResults.Add(new AnalysisResultItem { Method = method, Value = value });

        private void SaveReport()
        {
            if (string.IsNullOrEmpty(ImagePath)) return;

            var saveDialog = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt",
                Title = "Сохранить отчёт",
                FileName = $"StegoReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() != true) return;

            try
            {
                using var writer = new StreamWriter(saveDialog.FileName);
                writer.WriteLine("ОТЧЁТ СТЕГОАНАЛИЗА");
                writer.WriteLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Файл: {Path.GetFileName(ImagePath)}");
                writer.WriteLine($"Путь: {ImagePath}");
                writer.WriteLine(new string('-', 50));
                writer.WriteLine($"Вердикт: {VerdictText}");
                writer.WriteLine($"Обоснование: {VerdictJustification}");
                writer.WriteLine(new string('-', 50));
                writer.WriteLine("Детальные результаты:");
                foreach (var item in AnalysisResults)
                    writer.WriteLine($"{item.Method}: {item.Value}");
                writer.WriteLine(new string('-', 50));
                MessageBox.Show("Отчёт сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAbout() => MessageBox.Show("Стегодетектор\nВерсия 1.0\n\nАлгоритмы: Хи-квадрат, RS-анализ, DCT, Вейвлет-анализ", "О программе", MessageBoxButton.OK, MessageBoxImage.Information);

        // Отдельные методы 
        private void RunChiSquareOnly()
        {
            if (string.IsNullOrEmpty(ImagePath)) return;
            AnalysisResults.Clear();
            HistogramModel = null;
            IsHistogramVisible = false;

            try
            {
                var chi = new ChiSquareAnalyzer().Analyze(ImagePath);
                AddResult("Хи-квадрат", chi.PValueText);
                if (chi.MinChannelPValue < 0.05)
                {
                    VerdictText = "Обнаружено встраивание";
                    VerdictColor = "#D32F2F";
                    VerdictJustification = $"p-value = {chi.MinChannelPValue:F6} < 0.05, аномалия распределения пар значений.";
                }
                else
                {
                    VerdictText = "Встраивание не обнаружено";
                    VerdictColor = "#2E7D32";
                    VerdictJustification = $"p-value = {chi.MinChannelPValue:F6} >= 0.05, отклонений не выявлено.";
                }
                HistogramModel = new HistogramAnalyzer().GenerateChiSquareHistogram(ImagePath);
                IsHistogramVisible = true;
            }
            catch (Exception ex)
            {
                AddResult("Хи-квадрат", $"Ошибка: {ex.Message}");
                VerdictText = "Ошибка анализа";
                VerdictColor = "#FF9800";
                VerdictJustification = "Не удалось выполнить анализ.";
            }
            ShowVerdict = true;
        }

        private void RunRsOnly()
        {
            if (string.IsNullOrEmpty(ImagePath)) return;
            AnalysisResults.Clear();
            HistogramModel = null;
            IsHistogramVisible = false;

            try
            {
                var rs = new RsAnalyzer();
                var rsVal = rs.Analyze(ImagePath);
                if (rsVal.HasValue)
                {
                    AddResult("RS-анализ", $"{rsVal.Value:F2}%");
                    if (rsVal.Value > 30.0)
                    {
                        VerdictText = "Обнаружено встраивание";
                        VerdictColor = "#D32F2F";
                        VerdictJustification = $"Оценка заполнения = {rsVal.Value:F2}% > 30% — вероятно LSB-встраивание.";
                    }
                    else
                    {
                        VerdictText = "Встраивание не обнаружено";
                        VerdictColor = "#2E7D32";
                        VerdictJustification = $"Оценка заполнения = {rsVal.Value:F2}% <= 30%, признаков не выявлено.";
                    }
                }
                else
                {
                    AddResult("RS-анализ", "Ошибка вычислений");
                    VerdictText = "Ошибка анализа";
                    VerdictColor = "#FF9800";
                    VerdictJustification = "RS-анализ не смог вычислить оценку.";
                }
            }
            catch (Exception ex)
            {
                AddResult("RS-анализ", $"Ошибка: {ex.Message}");
                VerdictText = "Ошибка анализа";
                VerdictColor = "#FF9800";
                VerdictJustification = "Не удалось выполнить анализ.";
            }
            ShowVerdict = true;
        }

        private void RunDctOnly()
        {
            if (string.IsNullOrEmpty(ImagePath)) return;
            AnalysisResults.Clear();
            HistogramModel = null;
            IsHistogramVisible = false;

            try
            {
                var dct = new DctAnalyzer().Analyze(ImagePath);
                AddResult("DCT-анализ", dct.IsMessageDetected ? "Обнаружено" : "Не обнаружено");
                if (dct.IsMessageDetected)
                {
                    VerdictText = "Обнаружено встраивание";
                    VerdictColor = "#D32F2F";
                    VerdictJustification = $"Аномалия в коэффициентах ДКП (порог встраивания: {dct.EmbeddingThreshold:F2}).";
                }
                else
                {
                    VerdictText = "Встраивание не обнаружено";
                    VerdictColor = "#2E7D32";
                    VerdictJustification = "Аномалий в частотной области не выявлено.";
                }
            }
            catch (Exception ex)
            {
                AddResult("DCT-анализ", $"Ошибка: {ex.Message}");
                VerdictText = "Ошибка анализа";
                VerdictColor = "#FF9800";
                VerdictJustification = "Не удалось выполнить анализ.";
            }
            ShowVerdict = true;
        }

        private void RunWaveletOnly()
        {
            if (string.IsNullOrEmpty(ImagePath)) return;
            AnalysisResults.Clear();
            HistogramModel = null;
            IsHistogramVisible = false;

            try
            {
                var wavelet = new WaveletAnalyzer().Analyze(ImagePath);
                AddResult("Вейвлет-анализ (оценка длины)", $"{wavelet.EstimatedLengthPercent:F2}%");
                if (wavelet.EstimatedLengthPercent > 30.0)
                {
                    VerdictText = "Обнаружено встраивание";
                    VerdictColor = "#D32F2F";
                    VerdictJustification = $"Оценка длины сообщения = {wavelet.EstimatedLengthPercent:F2}% > 30%.";
                }
                else
                {
                    VerdictText = "Встраивание не обнаружено";
                    VerdictColor = "#2E7D32";
                    VerdictJustification = $"Оценка длины = {wavelet.EstimatedLengthPercent:F2}% <= 30%, аномалий не выявлено.";
                }
            }
            catch (Exception ex)
            {
                AddResult("Вейвлет-анализ", $"Ошибка: {ex.Message}");
                VerdictText = "Ошибка анализа";
                VerdictColor = "#FF9800";
                VerdictJustification = "Не удалось выполнить анализ.";
            }
            ShowVerdict = true;
        }

        // Все методы
        private void RunAnalysis()
        {
            if (string.IsNullOrEmpty(ImagePath))
            {
                MessageBox.Show("Нет загруженного изображения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AnalysisResults.Clear();
            HistogramModel = null;
            IsHistogramVisible = false;

            string format = Path.GetExtension(ImagePath)?.TrimStart('.').ToLowerInvariant() ?? "";

            bool chiAlert = false, rsAlert = false, dctAlert = false, waveletAlert = false;

            try
            {
                var chi = new ChiSquareAnalyzer().Analyze(ImagePath);
                AddResult("Хи-квадрат", chi.PValueText);
                chiAlert = chi.MinChannelPValue < 0.05;
            }
            catch (Exception ex) { AddResult("Хи-квадрат", $"Ошибка: {ex.Message}"); }

            try
            {
                var rs = new RsAnalyzer();
                var rsVal = rs.Analyze(ImagePath);
                if (rsVal.HasValue)
                {
                    AddResult("RS-анализ", $"{rsVal.Value:F2}%");
                    rsAlert = rsVal.Value > 30.0;
                }
                else AddResult("RS-анализ", "Ошибка вычислений");
            }
            catch (Exception ex) { AddResult("RS-анализ", $"Ошибка: {ex.Message}"); }

            try
            {
                var dct = new DctAnalyzer().Analyze(ImagePath);
                dctAlert = dct.IsMessageDetected;
                AddResult("DCT-анализ", dctAlert ? "Обнаружено" : "Не обнаружено");
            }
            catch (Exception ex) { AddResult("DCT-анализ", $"Ошибка: {ex.Message}"); }

            try
            {
                var wavelet = new WaveletAnalyzer().Analyze(ImagePath);
                AddResult("Вейвлет-анализ", $"{wavelet.EstimatedLengthPercent:F2}%");
                waveletAlert = wavelet.EstimatedLengthPercent > 30.0;
            }
            catch (Exception ex) { AddResult("Вейвлет-анализ", $"Ошибка: {ex.Message}"); }

            try
            {
                HistogramModel = new HistogramAnalyzer().GenerateChiSquareHistogram(ImagePath);
            }
            catch (Exception ex) { AddResult("Гистограмма", $"Ошибка: {ex.Message}"); }

            bool stegoDetected;
            string justification;

            if (format == "png" || format == "bmp" || format == "gif")
            {
                stegoDetected = (chiAlert && rsAlert) || waveletAlert;
                justification = $"Режим: пространственные методы. Хи-квадрат: {(chiAlert ? "аномалия" : "норма")}, RS: {(rsAlert ? "аномалия" : "норма")}, Вейвлет: {(waveletAlert ? "аномалия" : "норма")}.";
            }
            else if (format == "jpg" || format == "jpeg")
            {
                stegoDetected = dctAlert && waveletAlert;
                justification = $"Режим: частотные методы. DCT: {(dctAlert ? "аномалия" : "норма")}, Вейвлет: {(waveletAlert ? "аномалия" : "норма")}.";
            }
            else
            {
                stegoDetected = (chiAlert && rsAlert) || dctAlert || waveletAlert;
                justification = $"Режим: общий. Хи-квадрат+RS: {((chiAlert && rsAlert) ? "аномалия" : "норма")}, DCT: {(dctAlert ? "аномалия" : "норма")}, Вейвлет: {(waveletAlert ? "аномалия" : "норма")}.";
            }

            if (stegoDetected)
            {
                VerdictText = "Обнаружено встраивание";
                VerdictColor = "#D32F2F";
                VerdictJustification = justification + " Признаки скрытой информации обнаружены.";
            }
            else
            {
                VerdictText = "Встраивание не обнаружено";
                VerdictColor = "#2E7D32";
                VerdictJustification = justification + " Признаков не выявлено.";
            }

            ShowVerdict = true;
            ShowAnalysisButton = false;
            IsHistogramVisible = true;
        }
    }

    public class AnalysisResultItem
    {
        public string Method { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}