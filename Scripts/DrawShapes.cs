using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Scripts
{
    public static class DrawShapes
    {

        private static Canvas Drawing(Brush backgroundColor, Brush foregroundColor, int lineHeight, int width, string data)
        {
            Canvas canvDrawing = new Canvas();
            Path pathDrawing = new Path();
            canvDrawing.Children.Add(pathDrawing);
            pathDrawing.Data = (Geometry)TypeDescriptor.GetConverter(typeof(Geometry)).ConvertFrom(data);
            pathDrawing.Fill = foregroundColor;
            canvDrawing.Width = width + 2;
            canvDrawing.Height = lineHeight;
            canvDrawing.Background = backgroundColor;
            Canvas canvDrawing0 = new Canvas();
            canvDrawing0.Children.Add(canvDrawing);
            canvDrawing0.Width = width;
            canvDrawing0.Height = lineHeight;
            return canvDrawing0;
        }

        private static Canvas Triangle(Brush color, int width, string data)
        {
            Canvas canvTriangle = new Canvas();
            Path pathTriangle = new Path();
            canvTriangle.Children.Add(pathTriangle);
            pathTriangle.Data = (Geometry)TypeDescriptor.GetConverter(typeof(Geometry)).ConvertFrom(data);
            pathTriangle.Fill = color;
            canvTriangle.Width = width;
            return canvTriangle;
        }

        public static Canvas Line(Brush color, int lineHeight, double angle, double cornerRadius1, double cornerRadius2, int sideMargin, int height)
        {
            Canvas canvLine = new Canvas();
            Path pathLine = new Path();
            canvLine.Children.Add(pathLine);
            canvLine.Width = 2 * sideMargin;
            pathLine.Data = (Geometry)TypeDescriptor.GetConverter(typeof(Geometry)).ConvertFrom(
                "M 0," + ((int)Math.Round((lineHeight- height)/2+2*cornerRadius1- cornerRadius1 * Math.Sin(angle),0)).ToString()
                );
            pathLine.Fill = color;
            return canvLine;
        }

        public static Canvas TriangleLeftTop(Brush color, int lineHeight, double angle, double cornerRadius1, double cornerRadius2, int sideMargin)
        {
            int addWidth = (int)Math.Round(cornerRadius2 * Math.Sin(angle) - (cornerRadius2 - cornerRadius2 * Math.Cos(angle)) / Math.Tan(angle), 0);
            double width = (int)Math.Round(lineHeight / Math.Tan(angle), 0) + addWidth + sideMargin;
            string data = "M " + (width + 1).ToString() + "," + lineHeight.ToString() + " H " + width.ToString() +
                         " A " + cornerRadius2.ToString() + "," + cornerRadius2.ToString() + " 0 0 1 " +
                            ((int)Math.Round(width - cornerRadius2 * Math.Sin(angle), 0)).ToString() + "," +
                            ((int)Math.Round(lineHeight - cornerRadius2 + cornerRadius2 * Math.Cos(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(sideMargin + (cornerRadius1 + cornerRadius1 * Math.Cos(angle)) / Math.Tan(angle), 0)).ToString() + " " +
                            ((int)Math.Round(cornerRadius1 + cornerRadius1 * Math.Cos(angle), 0)).ToString() +
                         " A " + cornerRadius1.ToString() + "," + cornerRadius1.ToString() + " 0 0 1 " +
                            ((int)Math.Round(sideMargin + (cornerRadius1 + cornerRadius1 * Math.Cos(angle)) / Math.Tan(angle) + cornerRadius1 * Math.Sin(angle), 0)).ToString() + ",0" +
                         " H " + (width+1).ToString();
            Canvas canvTriangle = Triangle(color, (int)width, data);
            return canvTriangle;
        }

        public static Canvas TriangleLeftBottom(Brush color, int lineHeight, double angle, double cornerRadius1, double cornerRadius2, int sideMargin)
        {
            int addWidth = (int)Math.Round(cornerRadius2 * Math.Sin(angle) - (cornerRadius2 - cornerRadius2 * Math.Cos(angle)) / Math.Tan(angle), 0);
            double width = (int)Math.Round(lineHeight / Math.Tan(angle), 0) + addWidth + sideMargin;
            string data = "M " + (width + 1).ToString() + ",0" + " H " + width.ToString() +
                         " A " + cornerRadius2.ToString() + "," + cornerRadius2.ToString() + " 0 0 0 " +
                            ((int)Math.Round(width - cornerRadius2*Math.Sin(angle), 0)).ToString() + "," +
                            ((int)Math.Round(cornerRadius2-cornerRadius2*Math.Cos(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(sideMargin + (cornerRadius1+cornerRadius1*Math.Cos(angle))/Math.Tan(angle), 0)).ToString() + " " +
                            ((int)Math.Round(lineHeight - cornerRadius1-cornerRadius1*Math.Cos(angle), 0)).ToString() +
                         " A " + cornerRadius1.ToString() + "," + cornerRadius1.ToString() + " 0 0 0 " +
                            ((int)Math.Round(sideMargin + (cornerRadius1+cornerRadius1*Math.Cos(angle))/Math.Tan(angle) + cornerRadius1*Math.Sin(angle), 0)).ToString() + "," +
                            lineHeight.ToString() +
                         " H " + (width + 1).ToString();
            Canvas canvTriangle = Triangle(color, (int)width, data);
            return canvTriangle;
        }

        public static Canvas TriangleRightTop(Brush color, int lineHeight, double angle, double cornerRadius1, double cornerRadius2, int sideMargin)
        {
            int addWidth = (int)Math.Round(cornerRadius2 * Math.Sin(angle) - (cornerRadius2 - cornerRadius2 * Math.Cos(angle)) / Math.Tan(angle), 0);
            double width = (int)Math.Round(lineHeight / Math.Tan(angle), 0) + addWidth + sideMargin;
            string data = "M -1," + lineHeight.ToString() + " H 0" +
                         " A " + cornerRadius2.ToString() + "," + cornerRadius2.ToString() + " 0 0 0 " +
                            ((int)Math.Round(cornerRadius2 * Math.Sin(angle), 0)).ToString() + "," +
                            ((int)Math.Round(lineHeight - cornerRadius2 + cornerRadius2 * Math.Cos(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(width - sideMargin - (cornerRadius1 + cornerRadius1 * Math.Cos(angle)) / Math.Tan(angle), 0)).ToString() + " " +
                            ((int)Math.Round(cornerRadius1 + cornerRadius1 * Math.Cos(angle), 0)).ToString() +
                         " A " + cornerRadius1.ToString() + "," + cornerRadius1.ToString() + " 0 0 0 " +
                            ((int)Math.Round(width - sideMargin - (cornerRadius1 + cornerRadius1 * Math.Cos(angle)) / Math.Tan(angle) - cornerRadius1 * Math.Sin(angle), 0)).ToString() + ",0" +
                         " H -1";
            Canvas canvTriangle = Triangle(color, (int)width, data);
            return canvTriangle;
        }

        public static Canvas TriangleRightBottom(Brush color, int lineHeight, double angle, double cornerRadius1, double cornerRadius2, int sideMargin)
        {
            int addWidth = (int)Math.Round(cornerRadius2 * Math.Sin(angle) - (cornerRadius2 - cornerRadius2 * Math.Cos(angle)) / Math.Tan(angle), 0);
            double width = (int)Math.Round(lineHeight / Math.Tan(angle), 0) + addWidth + sideMargin;
            string data = "M -1,0" + " H 0" +
                         " A " + cornerRadius2.ToString() + "," + cornerRadius2.ToString() + " 0 0 1 " +
                            ((int)Math.Round(cornerRadius2 * Math.Sin(angle), 0)).ToString() + "," +
                            ((int)Math.Round(cornerRadius2 - cornerRadius2 * Math.Cos(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(width - sideMargin - (cornerRadius1 + cornerRadius1 * Math.Cos(angle)) / Math.Tan(angle), 0)).ToString() + " " +
                            ((int)Math.Round(lineHeight - cornerRadius1 - cornerRadius1 * Math.Cos(angle), 0)).ToString() +
                         " A " + cornerRadius1.ToString() + "," + cornerRadius1.ToString() + " 0 0 1 " +
                            ((int)Math.Round(width - sideMargin - (cornerRadius1 + cornerRadius1 * Math.Cos(angle)) / Math.Tan(angle) - cornerRadius1 * Math.Sin(angle), 0)).ToString() + "," +
                            lineHeight.ToString() +
                         " H -1";
            Canvas canvTriangle = Triangle(color, (int)width, data);
            return canvTriangle;
        }

        public static Canvas ArrowUp(Brush backgroundColor, Brush foregroundColor, int lineHeight, double width, double height, double strokewidth, int sideMargin)
        {
            double shiftX = sideMargin;
            double shiftY = (lineHeight - height) / 2;
            double angle = Math.Atan(2 * height / width);
            string data = "M " + ((int)Math.Round(shiftX, 0)).ToString() + "," + ((int)Math.Round(shiftY + height, 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + width / 2, 0)).ToString() + "," + ((int)Math.Round(shiftY, 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + width, 0)).ToString() + "," + ((int)Math.Round(shiftY + height, 0)).ToString() +
                         " H " + ((int)Math.Round(shiftX + width - strokewidth / Math.Sin(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + width / 2, 0)).ToString() + "," + ((int)Math.Round(shiftY + Math.Tan(angle) * strokewidth / Math.Sin(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + strokewidth / Math.Sin(angle), 0)).ToString() + "," + ((int)Math.Round(shiftY + height, 0)).ToString();
            Canvas canvArrow = Drawing(backgroundColor, foregroundColor, lineHeight, (int)width + sideMargin * 2, data);
            return canvArrow;
        }

        public static Canvas ArrowDown(Brush backgroundColor, Brush foregroundColor, int lineHeight, double width, double height, double strokewidth, int sideMargin)
        {
            double shiftX = sideMargin;
            double shiftY = (lineHeight - height) / 2;
            double angle = Math.Atan(2 * height / width);
            string data = "M " + ((int)Math.Round(shiftX, 0)).ToString() + "," + ((int)Math.Round(shiftY, 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + width / 2, 0)).ToString() + "," + ((int)Math.Round(shiftY + height, 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + width, 0)).ToString() + "," + ((int)Math.Round(shiftY, 0)).ToString() +
                         " H " + ((int)Math.Round(shiftX + width - strokewidth / Math.Sin(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + width / 2, 0)).ToString() + "," + ((int)Math.Round(shiftY + height - Math.Tan(angle) * strokewidth / Math.Sin(angle), 0)).ToString() +
                         " L " + ((int)Math.Round(shiftX + strokewidth / Math.Sin(angle), 0)).ToString() + "," + ((int)Math.Round(shiftY, 0)).ToString();
            Canvas canvArrow = Drawing(backgroundColor, foregroundColor, lineHeight, (int)width + sideMargin * 2, data);
            return canvArrow;
        }
    }
}
