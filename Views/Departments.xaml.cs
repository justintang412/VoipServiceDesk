using Arco.Models;
using Arco.Services;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Arco.Views
{
    /// <summary>
    /// Departments.xaml 的交互逻辑
    /// </summary>
    public partial class Departments : UserControl
    {
        private MainWindowViewModel _viewModel = null;
        public Departments()
        {
            InitializeComponent();
        }

        public Department FatherDepartment { get; set; }

        public Department NewDepartment { get; set; }
        public Department ChosenDepartment { get; set; }

        public ObservableCollection<Department> MyDepartments { get; set; }
        private CustomDialog CustomDialogAdd { get; set; }

        private async void CustomDialogAddClose(object sender, RoutedEventArgs e)
        {
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
        }
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            NewDepartment = new Department();
            FatherDepartment = null;
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "新增");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenDepartment == null)
            {
                _viewModel.Message = "请选择一个部门";
                return;
            }
            NewDepartment = ChosenDepartment;
            FatherDepartment = new List<Department>(MyDepartments).Find(x => x.Name.Equals(ChosenDepartment.Father));

            _viewModel.NotifyChange("ViewDepartments");
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

            if (CustomDialogAdd == null)
            {
                CustomDialogAdd = new CustomDialog(mainWindow.MetroDialogOptions)
                {
                    Content = this.Resources["CustomDialogAdd"],
                    DialogContentMargin = new GridLength(0),
                    Width = 600,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.DarkGray,
                    Background = new SolidColorBrush(Color.FromRgb(16, 57, 77)),//#10394d
                    DialogTop = this.Resources["CustomDialogAddClose"]
                };
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改");
                (topGrid.Children[2] as Button).Click += CustomDialogAddClose;
            }
            else
            {
                Grid topGrid = CustomDialogAdd.DialogTop as Grid;
                topGrid.FindChild<Label>().SetCurrentValue(ContentProperty, "修改");
            }
            await mainWindow.ShowMetroDialogAsync(CustomDialogAdd);

        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenDepartment == null)
            {
                _viewModel.Message = "请选择一个部门";
                return;
            }
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", ChosenDepartment.Id);
            IMongoCollection<BsonDocument> mongoColleciton = DataService.GetInstance().Database.GetCollection<BsonDocument>("Department");
            mongoColleciton.DeleteOne(filter);
            ChosenDepartment = null;

            LoadDepartments();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (DataContext as MainWindowViewModel);
            _viewModel.ViewDepartments = this;
            LoadDepartments();
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ProcessAtControl = null;
            _viewModel.ViewDepartments = null;
        }

        private async void Department_Datagrid_Seletion_Changed(object sender, SelectionChangedEventArgs e)
        {
            object obj = (sender as DataGrid)?.SelectedItem;
            if (obj == null) return;
            ChosenDepartment = obj as Department;
            _viewModel.NotifyChange("ViewDepartments");
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (NewDepartment == null||
                NewDepartment.Name==null) return;

            _viewModel.Message = "正在保存部门信息...";

            NewDepartment.Father = FatherDepartment == null ? "" : FatherDepartment.Name;
            long count = DataService.GetInstance().Database.GetCollection<BsonDocument>("Department").Find(Builders<BsonDocument>
                .Filter.Eq("_id", NewDepartment.Id))
                .CountDocuments();
            if (count > 0)
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", NewDepartment.Id);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                    .Set("Name", NewDepartment.Name??"")
                    .Set("Rank", NewDepartment.Rank)
                    .Set("Father", NewDepartment.Father ?? "")
                    .Set("Remark", NewDepartment.Remark ?? "");
                DataService.GetInstance().Database.GetCollection<BsonDocument>("Department").UpdateOne(filter, update);
            }
            else
            {
                BsonDocument bsonDocument = new BsonDocument {
                    {"Name",NewDepartment.Name??"" },
                    { "Rank", NewDepartment.Rank },
                    { "Remark", NewDepartment.Remark??"" },
                    { "Father", NewDepartment.Father??""}
                };
                DataService.GetInstance().Database.GetCollection<BsonDocument>("Department").InsertOne(bsonDocument);
            }
            await ((MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(CustomDialogAdd);
            LoadDepartments();
            _viewModel.Message += "成功";
        }


        private void LoadDepartments()
        {
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Empty;


            MyDepartments = new ObservableCollection<Department>(
                    DataService.GetInstance().Database.GetCollection<BsonDocument>("Department")
                        .Find(filter)
                        .Sort("{Rank:1}")
                        .ToList()
                        .Select(x =>
                        {
                            Department department = new Department
                            {
                                Id = (ObjectId)x["_id"],
                                Name = (string)x["Name"],
                                Rank = (Int16)x["Rank"],
                                Father = (string)x["Father"],
                                Remark = (string)x["Remark"]
                            };
                            return department;

                        })
                    );
            this._viewModel.Departments = MyDepartments;
            _viewModel.NotifyChange("ViewDepartments");
        }

    }
}
