using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// ViewPositions.xaml 的交互逻辑
    /// </summary>
    public partial class ViewPositions : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public ViewPositions()
        {
            InitializeComponent();
        }
        
        public Models.Position NewPosition { get; set; }
        public Models.Position ChosenPosition { get; set; }

        public ObservableCollection<Models.Position> MyPositions { get; set; }
        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewPosition = new Models.Position();
            
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 500,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPosition == null)
            {
                _viewModel.Message = "请选择一个岗位";
                return;
            }
            NewPosition = ChosenPosition;
            
            _viewModel.NotifyChange("ViewPositions");
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(5),
                    Width = 500,
                    Height = 200,
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.DarkGray,
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }

            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);

        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPosition == null)
            {
                _viewModel.Message = "请选择一个岗位";
                return;
            }
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", ChosenPosition.Id);
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("Position");
            mongoColleciton.DeleteOne(filter);
            ChosenPosition = null;

            LoadPositions();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.ViewPositions = this;
            LoadPositions();
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.ViewPositions = null;
        }

 

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (NewPosition == null|| NewPosition.Name==null) return;
            _viewModel.Message = "正在保存岗位信息...";

           long count = DataService.GetInstance().Database.GetCollection<BsonDocument>("Position").Find(Builders<BsonDocument>
                .Filter.Eq("_id", NewPosition.Id))
                .CountDocuments();
            if (count > 0)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Id", NewPosition.Id);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                    .Set("Name", NewPosition.Name)
                    .Set("Rank", NewPosition.Rank)
                    .Set("Remark", NewPosition.Remark??"");
                DataService.GetInstance().Database.GetCollection<BsonDocument>("Position").UpdateOne(filter, update);
            }
            else
            {
                BsonDocument bsonDocument = new BsonDocument {
                    {"Name",NewPosition.Name },
                    { "Rank", NewPosition.Rank },
                    { "Remark", NewPosition.Remark??"" }
                };
                DataService.GetInstance().Database.GetCollection<BsonDocument>("Position").InsertOne(bsonDocument);
            }
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
            LoadPositions();
            _viewModel.Message += "成功";
        }


        private void LoadPositions()
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Empty;


            MyPositions = new ObservableCollection<Models.Position>(
                    DataService.GetInstance().Database.GetCollection<BsonDocument>("Position")
                        .Find(filter)
                        .Sort("{Rank:1}")
                        .ToList()
                        .Select(x =>
                        {
                            Models.Position position = new Models.Position
                            {
                                Id = (ObjectId)x["_id"],
                                Name = (string)x["Name"],
                                Rank = (Int16)x["Rank"],
                                Remark = (string)x["Remark"]
                            };
                            return position;

                        })
                    );
            _viewModel.Positions = MyPositions;
            _viewModel.NotifyChange("ViewPositions");
        }

        private void Position_Datagrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            object obj = (sender as DataGrid)?.SelectedItem;
            if (obj == null) return;
            ChosenPosition = obj as Models.Position;
            _viewModel.NotifyChange("ViewPositions");
        }
    }
}
