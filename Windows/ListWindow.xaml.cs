using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GTRCLeagueManager
{
    public partial class ListWindow : Window
    {

        public RowDefinition NewRow;
        public ColumnDefinition NewColumn;
        public Label NewRowDataLabel;
        public List<Label> NewColumnDataLabel;
        public List<List<Label>> DataLabels = new List<List<Label>> { };
        public List<string> drawShapeTypes = new List<string>();

        private int gapTop = 50;
        private int lineHeight = 30;
        private int lineSpace = 5;
        private int strokeHeight = 20;
        private int angle = 65;
        private int cornerRadius1 = 5;
        private int cornerRadius2 = 5;
        private int logoPadding = 2;
        private int sideMargin = 5;
        private int arrowWidth = 12;
        private int arrowHeight = 7;
        private int arrowStrokeWidth = 2;
        private Brush color0 = (Brush)Application.Current.FindResource("color0");
        private Brush color1 = (Brush)Application.Current.FindResource("color1");
        private Brush color2 = (Brush)Application.Current.FindResource("color2");
        private Brush color3 = (Brush)Application.Current.FindResource("color3");
        private Brush color4 = (Brush)Application.Current.FindResource("color4");

        public ListWindow()
        {
            InitializeComponent();
            SizeToContent = SizeToContent.WidthAndHeight;

            drawShapeTypes.Add("Line");
            drawShapeTypes.Add("TriangleLeftTop");
            drawShapeTypes.Add("TriangleLeftBottom");
            drawShapeTypes.Add("TriangleRightTop");
            drawShapeTypes.Add("TriangleRightBottom");
            drawShapeTypes.Add("ArrowUp");
            drawShapeTypes.Add("ArrowDown");
        }

        public void ShowWindow()
        {
            Top = 0;
            MaxHeight = MainWindow.screenHeight;
            Show();
            Left = (MainWindow.screenWidth - Width) / 2;
        }

        public void ShowHiddenWindow()
        {
            Top = MainWindow.screenHeight;
            Left = MainWindow.screenWidth;
            Show();
        }

        public void AddTable()
        {
            NewRow = new RowDefinition();
            ResultsGrid.RowDefinitions.Add(NewRow);
            NewRow.Height = new GridLength(gapTop, GridUnitType.Pixel);

            for (int resultsLine = 0; resultsLine < ResultsLine.ResultsLineList.Count; resultsLine++)
            {
                NewRow = new RowDefinition();
                ResultsGrid.RowDefinitions.Add(NewRow);
                NewRow.Height = new GridLength(lineHeight, GridUnitType.Pixel);

                NewRow = new RowDefinition();
                ResultsGrid.RowDefinitions.Add(NewRow);
                NewRow.Height = new GridLength(lineSpace, GridUnitType.Pixel);
            }

            for (int tableDesignNr = 1; tableDesignNr < RuleVM.ruleList[0].TableDesignList.Count; tableDesignNr++)
            {
                NewColumn = new ColumnDefinition();
                ResultsGrid.ColumnDefinitions.Add(NewColumn);
                NewColumn.Width = new GridLength(1, GridUnitType.Auto);
                NewColumnDataLabel = new List<Label> { };
                DataLabels.Add(NewColumnDataLabel);

                TableDesign tableDesign = RuleVM.ruleList[0].TableDesignList[tableDesignNr];
                Style style = Application.Current.FindResource("LabelStyle") as Style;

                for (int resultsLineNr = 0; resultsLineNr < ResultsLine.ResultsLineList.Count; resultsLineNr++)
                {
                    NewRowDataLabel = new Label { };
                    NewColumnDataLabel.Add(NewRowDataLabel);

                    Dictionary<string, dynamic> dict = ResultsLine.ResultsLineList[resultsLineNr].ReturnAsDict();
                    AddLabel(resultsLineNr, tableDesignNr - 1, tableDesign.ColumnName, dict[tableDesign.ColumnName], style);
                }
            }
        }

        public void AddLabel(int rowNr, int columnNr, string propertyName, dynamic Content, Style style)
        {
            Image logo;
            int rowNrWindow = 1 + rowNr * 2;
            string strContent = "";

            if (Content is IList)
            {
                foreach (var item in Content) { strContent += item.ToString() + ", "; }
                if (strContent.Length > 0) { strContent = strContent.Substring(0, strContent.Length - 2); }
            }
            else { strContent = Content.ToString(); }

            //Allgemein
            DataLabels[columnNr][rowNr].Content = strContent;
            DataLabels[columnNr][rowNr].Style = style;
            ResultsGrid.Children.Add(DataLabels[columnNr][rowNr]);
            Grid.SetColumn(DataLabels[columnNr][rowNr], columnNr);
            Grid.SetRow(DataLabels[columnNr][rowNr], rowNrWindow);

            //Fahrzeug Logo
            if (propertyName == "Entry Car Logo" || propertyName == "OnServer Car Logo")
            {
                string pathLogo = "";
                if (DataLabels[columnNr][rowNr].Content != null) { pathLogo = DataLabels[columnNr][rowNr].Content.ToString(); }
                if (!File.Exists(pathLogo)) { DataLabels[columnNr][rowNr].Content = ""; }
                else
                {
                    logo = new Image();
                    logo.Height = lineHeight - Math.Min(2 * logoPadding, lineHeight);
                    logo.Source = new BitmapImage(new Uri(pathLogo, UriKind.Absolute));
                    DataLabels[columnNr][rowNr].Content = logo;
                    Thickness padding = DataLabels[columnNr][rowNr].Padding;
                    padding.Top = logoPadding;
                    padding.Bottom = logoPadding;
                    padding.Left = logoPadding;
                    padding.Right = logoPadding;
                    DataLabels[columnNr][rowNr].Padding = padding;
                }
            }

            //DrawShapes
            if (drawShapeTypes.Contains(propertyName))
            {
                DataLabels[columnNr][rowNr].Style = Application.Current.FindResource("LabelDrawingStyle") as Style;
                switch (propertyName)
                {
                    case "Line":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.Line(color1, lineHeight, (angle * Math.PI / 180), cornerRadius1, cornerRadius2, sideMargin, strokeHeight);
                        break;

                    case "TriangleLeftTop":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.TriangleLeftTop(color1, lineHeight, (angle * Math.PI / 180), cornerRadius1, cornerRadius2, sideMargin);
                        break;

                    case "TriangleLeftBottom":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.TriangleLeftBottom(color1, lineHeight, (angle * Math.PI / 180), cornerRadius1, cornerRadius2, sideMargin);
                        break;

                    case "TriangleRightTop":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.TriangleRightTop(color1, lineHeight, (angle * Math.PI / 180), cornerRadius1, cornerRadius2, sideMargin);
                        break;

                    case "TriangleRightBottom":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.TriangleRightBottom(color1, lineHeight, (angle * Math.PI / 180), cornerRadius1, cornerRadius2, sideMargin);
                        break;

                    case "ArrowUp":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.ArrowUp(color1, color2, lineHeight, arrowWidth, arrowHeight, arrowStrokeWidth, sideMargin);
                        break;

                    case "ArrowDown":
                        DataLabels[columnNr][rowNr].Content = DrawShapes.ArrowDown(color1, color2, lineHeight, arrowWidth, arrowHeight, arrowStrokeWidth, sideMargin);
                        break;
                }
            }
        }
    }
}
