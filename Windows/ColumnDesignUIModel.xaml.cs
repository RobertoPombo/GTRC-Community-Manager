using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GTRCLeagueManager.Windows
{
    public partial class ColumnDesignUIModel : UserControl
    {
        public ColumnDesignUIModel()
        {
            InitializeComponent();
        }
        /*
        private void NewDesign(object sender, RoutedEventArgs e)
        {
            Button Sender = sender as Button;
            int countRows = DesignGrid.RowDefinitions.Count;
            int columnNr = Grid.GetColumn(Sender) + 2;
            int newRowNr = TableDesign.TableDesignList.Count;
            double newColumnWidth = DesignGrid.ColumnDefinitions[columnNr].ActualWidth;
            double newRowHeight = DesignGrid.RowDefinitions[newRowNr - 1].ActualHeight;
            if (newRowNr == countRows - 1)
            {
                RowDefinition NewRow = new RowDefinition();
                DesignGrid.RowDefinitions.Insert(newRowNr, NewRow);
                NewRow.Height = new GridLength(newRowHeight, GridUnitType.Pixel);
            }
            Thickness margin;
            RadioButton newRadioButton = new RadioButton();
            ComboBox newComboBox = new ComboBox();
            Button newDelButton = new Button();
            newRadioButton.Name = "Design" + newRowNr.ToString();
            newRadioButton.HorizontalAlignment = Design0.HorizontalAlignment; newRadioButton.VerticalAlignment = Design0.VerticalAlignment;
            margin = Design0.Margin; newRadioButton.Margin = margin;
            newRadioButton.Click += SelectDesign;
            newRadioButton.Content = newComboBox;
            newRadioButton.Style = Design0.Style;
            newComboBox.Name = "ColumnName" + newRowNr.ToString();
            newComboBox.SelectionChanged += ChangeDesign;
            newComboBox.Style = Application.Current.FindResource("ComboBoxTableDesignStyle") as Style;
            newDelButton.Name = "DelDesign" + newRowNr.ToString();
            newDelButton.HorizontalAlignment = HorizontalAlignment.Stretch; newDelButton.VerticalAlignment = VerticalAlignment.Stretch;
            margin = new Thickness(5); newDelButton.Margin = margin;
            newDelButton.Style = Application.Current.FindResource("DelButtonStyle") as Style;
            DesignGrid.Children.Add(newRadioButton);
            Grid.SetColumn(newRadioButton, columnNr);
            Grid.SetRow(newRadioButton, newRowNr);
            DesignGrid.Children.Add(newDelButton);
            Grid.SetColumn(newDelButton, columnNr - 2);
            Grid.SetRow(newDelButton, newRowNr);
            NewDesignAddItems(newComboBox);
            newComboBox.Focus();
        }

        private void NewDesignAddItems(ComboBox newComboBox)
        {
            TableDesign tableDesign = new TableDesign { TableDesignIsInDatabase = true };
            newComboBox.Items.Clear();
            Dictionary<string, dynamic> dict = ResultsLine.ResultsLineListTemp[0].ReturnAsDict();
            foreach (string key in dict.Keys) { newComboBox.Items.Add(key); }
            if (newComboBox.Items.Count > 0)
            {
                newComboBox.SelectedValue = tableDesign.ColumnName;
            }
        }

        private void SelectDesign(object sender, RoutedEventArgs e)
        {
            RadioButton Sender = sender as RadioButton;
            if (Sender.Content.GetType() == typeof(ComboBox))
            {
                ComboBox comboBox = Sender.Content as ComboBox;
                comboBox.Focus();
            }
        }

        private void ChangeDesign(object sender, RoutedEventArgs e)
        {
            ComboBox Sender = sender as ComboBox;
            int rowNr = Grid.GetRow(Sender.Parent as UIElement);
            if (rowNr < TableDesign.TableDesignList.Count) { TableDesign.TableDesignList[rowNr].ColumnName = Sender.SelectedValue.ToString(); }
        }

        private void HideStandard(object sender, RoutedEventArgs e)
        {
            if (ButtonHideStandard.Content.ToString() == "Show Standards") { ButtonHideStandard.Content = "Hide Standards"; }
            else { ButtonHideStandard.Content = "Show Standards"; }
        }

        private void Prop2Standard(object sender, RoutedEventArgs e) { }
        */
    }
}
