using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Lab01;
using LiveCharts;
using LiveCharts.Wpf;

namespace Lab01
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker = new BackgroundWorker();

        string filename;
        ObservableCollection<Person> people = new ObservableCollection<Person>()
        {
            new Person { Name = "Warsaw", Age = 290 },
            new Person { Name = "Paris", Age = 291 },
            new Person { Name = "London", Age = 288 }
        };


        public ObservableCollection<Person> Items
        {
            get => people;
        }

        public object ProgresChanged { get; private set; }
        Lab01.Entity_Data_Modells.WeatherEntities db = new Lab01.Entity_Data_Modells.WeatherEntities();
        System.Windows.Data.CollectionViewSource weatherEntryViewSource;
        System.Windows.Data.CollectionViewSource weatherEntitiesViewSource;

        public MainWindow()
        {

            InitializeComponent();

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;

            weatherEntryViewSource =
               ((System.Windows.Data.CollectionViewSource)(this.FindResource("weatherEntryViewSource")));
            weatherEntitiesViewSource =
                ((System.Windows.Data.CollectionViewSource)(this.FindResource("weatherEntitiesViewSource")));

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Temperature",
                    Values = new ChartValues<int> { people.Where(c => c.Name == "Warsaw").Select(c => c.Age).Last()-273, people.Where(c => c.Name == "Paris").Select(c => c.Age).Last() - 273, people.Where(c => c.Name == "London").Select(c => c.Age).Last() - 273 }
                }
            };

            Labels = new[] { "Warsaw", "Paris", "London" };
            Formatter = value => value.ToString("N");
            DataContext = this;
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            db.WeatherEntries.Local.Concat(db.WeatherEntries.ToList());
            weatherEntryViewSource.Source = db.WeatherEntries.Local;
            weatherEntitiesViewSource.Source = db.WeatherEntries.Local;
            System.Windows.Data.CollectionViewSource personViewSource = ((System.Windows.Data.CollectionViewSource)(this.FindResource("personViewSource")));
        }

        public void AddPerson(Person person)
        {
            Application.Current.Dispatcher.Invoke(() => { Items.Add(person); });
        }

        private void AddNewPersonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                people.Add(new Person { Age = int.Parse(ageTextBox.Text), Name = nameTextBox.Text, Filename = filename });
            }
            catch (FormatException)
            {
                throw new FormatException("Age have to be a number");
            }
        }

        private void AddPictureButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            // fileDialog.DefaultExt = ".jpg";
            if (fileDialog.ShowDialog() == true)
            {
                filename = fileDialog.FileName;
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ShowPictureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BitmapImage myBitMap = new BitmapImage();
                myBitMap.BeginInit();
                myBitMap.UriSource = new Uri(filename);
                myBitMap.DecodePixelWidth = 200;
                myBitMap.EndInit();
                myImage.Source = myBitMap;
            }
            catch (ArgumentNullException)
            {
                throw new Exception("You have to load image");
            }
        }

        private async void AddTextButton_Click(object sender, RoutedEventArgs e)
        {
            Progress<int> progress = new Progress<int>();
            progress.ProgressChanged += ReportProgress;
            PersonalInfo personal = await GetApiAsync("https://uinames.com/api/?ext", progress);
        }

        async Task<PersonalInfo> GetApiAsync(string path, IProgress<int> progress)
        {
            int report = new int();
            PersonalInfo personal = null;
            int levelmax = 10;
            int presentLevel = 0;
            while(presentLevel<=10)
            {
                presentLevel++;
                report = (presentLevel * 100) / levelmax;
                progress.Report(report);
                using (HttpClient client = new HttpClient())
                {

                    using (HttpResponseMessage response = await client.GetAsync(path))
                    {
                        using (HttpContent content = response.Content)
                        {
                            var stringContent = await content.ReadAsStringAsync();
                            personal = JsonConvert.DeserializeObject<PersonalInfo>(stringContent);
                            try
                            {
                                people.Add(new Person { Age = personal.age, Name = personal.name, Filename = personal.photo });
                            }
                            catch (FormatException)
                            {
                                throw new FormatException("Age have to be a number");
                            }
                            catch (Exception)
                            {
                                throw new Exception("Can't load data from internet");
                            }
                        }
                    }
                }
                await Task.Delay(1000);               
            }

            return personal;
        }

        private void ReportProgress(object sender, int e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = e;
                });
            }
            catch { }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            weatherDataProgressBar.Value = e.ProgressPercentage;
            weatherDataTextBlock.Text = e.UserState as string;
        }

        private async void LoadWeatherData(object sender, RoutedEventArgs e)
        {
            string responseXML = await WeatherConnection.LoadData("London");
            WeatherDataEntry result;

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseXML)))
            {
                Person newOne = new Person();
                result = ParseWeather_LINQ.Parse(stream);
                try
                {
                    newOne.Name = result.City;
                    newOne.Age = (int)Math.Round(result.Temperature);

                    Items.Add(newOne);
                }
                catch (Exception)
                {
                    throw new Exception("Can't load informations about weather.");
                }

            }
            if (worker.IsBusy != true)
                worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<string> cities = new List<string> {
                "London", "Warsaw", "Paris", "London", "Warsaw" };
            for (int i = 1; i <= cities.Count; i++)
            {
                string city = cities[i - 1];

                if (worker.CancellationPending == true)
                {
                    worker.ReportProgress(0, "Cancelled");
                    e.Cancel = true;
                    return;
                }
                else
                {
                    worker.ReportProgress(
                        (int)Math.Round((float)i * 100.0 / (float)cities.Count),
                        "Loading " + city + "...");
                    string responseXML = WeatherConnection.LoadData(city).Result;
                    WeatherDataEntry result;

                    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseXML)))
                    {
                        result = ParseWeather_XmlReader.Parse(stream);
                        Person newOne = new Person();
                        try
                        {
                            newOne.Name = result.City;
                            newOne.Age = (int)Math.Round(result.Temperature);

                            AddPerson(newOne);
                        }
                        catch (Exception)
                        {
                            throw new Exception("Can't load informations about weather.");
                        }
                    }
                    Thread.Sleep(2000);
                }
            }
            worker.ReportProgress(100, "Done");
        }
        private void DrawGraphButton_Click(object sender, RoutedEventArgs e)
        {
            SeriesCollection.Add(new ColumnSeries
            {
                Title = "Temperature",
                Values = new ChartValues<int> { people.Where(c => c.Name == "Warsaw").Select(c => c.Age).Last() - 273, people.Where(c => c.Name == "Paris").Select(c => c.Age).Last() - 273, people.Where(c => c.Name == "London").Select(c => c.Age).Last() - 273 }
            });
            Labels = new[] { "Warsaw", "Paris", "London" };
            Formatter = value => value.ToString("N");
        }
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }
        private async void LoadCityTemp_Click(object sender, RoutedEventArgs e)
        {
            string city = cityTextBox.Text;
            string responseXML = await WeatherConnection.LoadData(city);
            WeatherDataEntry result;

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseXML)))
            {
                result = ParseWeather_LINQ.Parse(stream);
                Person newOne = new Person();
                var newEntry = new Lab01.Entity_Data_Modells.WeatherEntry();

                try
                {
                    newOne.Name = result.City;
                    newOne.Age = (int)Math.Round(result.Temperature);

                    Items.Add(newOne);
                }
                catch (Exception)
                {
                    throw new Exception("Can't load informations about weather.");
                }

                try
                {

                    newEntry.Id = int.Parse(idTextBox.Text);
                    newEntry.City = cityTextBox.Text;
                    newEntry.Temperature = newOne.Age;

                    db.WeatherEntries.Local.Add(newEntry);
                }
                catch (FormatException)
                {
                    throw new FormatException("Id have to be a number");
                }
                try
                {

                    if (db.WeatherEntries.Any(g => g.Id == newEntry.Id))
                        throw new Exception();
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    db.WeatherEntries.Local.Remove(newEntry);
                    throw new Exception("Error, id is not unique!");
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (worker.WorkerSupportsCancellation == true)
            {
                weatherDataTextBlock.Text = "Cancelling...";
                worker.CancelAsync();
            }
        }



        public class PersonalInfo
        {
            public string name { get; set; }
            public int age { get; set; }
            public string photo { get; set; }
        }



    }
}