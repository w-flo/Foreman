﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Foreman
{
	public class ItemTab : GraphElement
	{
		public LinkType Type;

		private const int iconSize = 32;
		private const int border = 4;
		private int textHeight = 11;

		public Color FillColour { get; set; }

		private string text = "";
		public String Text
		{
			get { return text; }
			set
			{
				text = value;
				textHeight = (int)Parent.CreateGraphics().MeasureString(value, new Font(FontFamily.GenericSansSerif, 7)).Height;
			}
		}

		public Item Item { get; private set; }

		public ItemTab(Item item, LinkType type, ProductionGraphViewer parent)
			: base(parent)
		{
			this.Item = item;
			this.Type = type;
		}
		
		public override System.Drawing.Point Size
		{
			get
			{
				return new Point(iconSize + border * 3, iconSize + textHeight + border * 3);
			}
			set
			{
			}
		}

		public override void Paint(Graphics graphics)
		{
			Point iconPoint = Point.Empty;
			if (Type == LinkType.Output)
			{
				iconPoint = new Point((int)(border * 1.5), Height - (int)(border * 1.5) - iconSize);
			}
			else
			{
				iconPoint = new Point((int)(border * 1.5), (int)(border * 1.5));
			}

			StringFormat centreFormat = new StringFormat();
			centreFormat.Alignment = centreFormat.LineAlignment = StringAlignment.Center;
			
			using (Pen borderPen = new Pen(Color.Gray, 3))
			using (Brush fillBrush = new SolidBrush(FillColour))
			using (Brush textBrush = new SolidBrush(Color.Black))
			{
				if (Type == LinkType.Output)
				{
					GraphicsStuff.FillRoundRect(0, 0, Width, Height, border, graphics, fillBrush);
					GraphicsStuff.DrawRoundRect(0, 0, Width, Height, border, graphics, borderPen);
					graphics.DrawString(Text, new Font(FontFamily.GenericSansSerif, 7), textBrush, new PointF(Width / 2, (textHeight + border) / 2), centreFormat);
				}
				else
				{
					GraphicsStuff.FillRoundRect(0, 0, Width, Height, border, graphics, fillBrush);
					GraphicsStuff.DrawRoundRect(0, 0, Width, Height, border, graphics, borderPen);
					graphics.DrawString(Text, new Font(FontFamily.GenericSansSerif, 7), textBrush, new PointF(Width / 2, Height - (textHeight + border) / 2), centreFormat);
				}
			}
			graphics.DrawImage(Item.Icon ?? DataCache.UnknownIcon, iconPoint.X, iconPoint.Y, iconSize, iconSize);
		}
	}
}