using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

namespace Foreman
{	
	public partial class MainForm : Form
	{

		public MainForm()
		{
			InitializeComponent();
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			string[] factorioPaths = new string[]
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Factorio"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Factorio"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Factorio"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "factorio"),
			};

			String factorioPath = null;
			foreach(string testPath in factorioPaths)
			{
				if(File.Exists(Path.Combine(testPath, "data", "core", "lualib", "dataloader.lua")))
				{
					Console.Out.WriteLine("Found factorio: "+testPath);
					factorioPath = testPath;
					break;
				}
			}

			if (factorioPath == null)
			{
				using (DirectoryChooserForm form = new DirectoryChooserForm())
				{
					if (form.ShowDialog() == DialogResult.OK)
					{
						factorioPath = form.SelectedPath;
					}
					else
					{
						Close();
						Dispose();
						return;
					}
				}
			}

			List<string> mods = new List<string>();
			string[] modPaths = new string[]
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Factorio", "mods"),
				Path.Combine(factorioPath, "mods"),
			};
			foreach(string modPath in modPaths)
			{
				if(Directory.Exists(modPath))
				{
					foreach(string mod in Directory.GetFiles(modPath, "*.zip", SearchOption.TopDirectoryOnly))
					{
						string modDirectory = UnzipMod(mod);
						Console.Out.WriteLine("unpacking "+mod+" to temporary location "+modDirectory);

						if(File.Exists(Path.Combine(modDirectory, "data.lua")))
						{
							mods.Add(modDirectory);
							Console.Out.WriteLine("added mod directory "+modDirectory);
						}
					}

					foreach(string modDirectory in Directory.GetDirectories(modPath, "*", SearchOption.TopDirectoryOnly))
					{
						if(File.Exists(Path.Combine(modDirectory, "data.lua")))
						{
							mods.Add(modDirectory);
							Console.Out.WriteLine("added mod directory "+modDirectory);
						}
					}
				}
			}

			DataCache.LoadRecipes(factorioPath, mods);

			rateOptionsDropDown.SelectedIndex = 0;

			AssemblerSelectionBox.Items.Clear();
			AssemblerSelectionBox.Items.AddRange(DataCache.Assemblers.Values.ToArray());
			AssemblerSelectionBox.Sorted = true;
			AssemblerSelectionBox.DisplayMember = "FriendlyName";
			for ( int i = 0; i < AssemblerSelectionBox.Items.Count; i++)
			{
				AssemblerSelectionBox.SetItemChecked(i, true);
			}

			MinerSelectionBox.Items.AddRange(DataCache.Miners.Values.ToArray());
			MinerSelectionBox.Sorted = true;
			MinerSelectionBox.DisplayMember = "FriendlyName";
			for (int i = 0; i < MinerSelectionBox.Items.Count; i++)
			{
				MinerSelectionBox.SetItemChecked(i, true);
			}

			ModuleSelectionBox.Items.AddRange(DataCache.Modules.Values.ToArray());
			ModuleSelectionBox.Sorted = true;
			ModuleSelectionBox.DisplayMember = "FriendlyName";
			for (int i = 0; i < ModuleSelectionBox.Items.Count; i++)
			{
				ModuleSelectionBox.SetItemChecked(i, true);
			}

			ItemListBox.Items.Clear();
			ItemListBox.Items.AddRange(DataCache.Items.Values.ToArray());
			ItemListBox.DisplayMember = "FriendlyName";
			ItemListBox.Sorted = true;
		}

		private string UnzipMod(string modFile)
		{
			string tempPath = Path.Combine(Path.GetTempPath(), "Foreman-Factorio", Path.GetRandomFileName());
			using(ZipInputStream stream = new ZipInputStream(File.OpenRead(modFile)))
			{
				ZipEntry entry;
				while((entry = stream.GetNextEntry()) != null)
				{
					string location = Path.Combine(tempPath, entry.Name);
					if(Path.GetFileName(location) == "") continue;
					Directory.CreateDirectory(Path.GetDirectoryName(location));

					using(FileStream writer = File.Create(location))
					{
						int size;
						byte[] data = new byte[2048];
						do {
							size = stream.Read(data, 0, data.Length);
							writer.Write(data, 0, size);
						} while(size > 0);
					}
				}
			}
			return Directory.EnumerateDirectories(tempPath).FirstOrDefault();
		}

		private void ItemListForm_KeyDown(object sender, KeyEventArgs e)
		{
#if DEBUG
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
#endif
		}

		private void AddItemButton_Click(object sender, EventArgs e)
		{
			GraphViewer.AddDemands(ItemListBox.SelectedItems.Cast<Item>());
		}
		
		private void rateButton_CheckedChanged(object sender, EventArgs e)
		{
			if ((sender as RadioButton).Checked)
			{
				this.GraphViewer.Graph.SelectedAmountType = AmountType.Rate;
				rateOptionsDropDown.Enabled = true;
			}
			else
			{
				rateOptionsDropDown.Enabled = false;
			}
			GraphViewer.UpdateNodes();
			GraphViewer.Invalidate();
		}

		private void fixedAmountButton_CheckedChanged(object sender, EventArgs e)
		{
			if ((sender as RadioButton).Checked)
			{
				this.GraphViewer.Graph.SelectedAmountType = AmountType.FixedAmount;
			}

				MinerDisplayCheckBox.Checked 
					= MinerDisplayCheckBox.Enabled
					= AssemblerDisplayCheckBox.Enabled
					= AssemblerDisplayCheckBox.Checked
					= !(sender as RadioButton).Checked;
			
			GraphViewer.UpdateNodes();
			GraphViewer.Invalidate();
		}

		private void rateOptionsDropDown_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch ((sender as ComboBox).SelectedIndex)
			{
				case 0:
					GraphViewer.Graph.SelectedUnit = RateUnit.PerSecond;
					GraphViewer.Invalidate();
					GraphViewer.UpdateNodes();
					break;
				case 1:
					GraphViewer.Graph.SelectedUnit = RateUnit.PerMinute;
					GraphViewer.Invalidate();
					GraphViewer.UpdateNodes();
					break;
			}
		}

		private void AutomaticCompleteButton_Click(object sender, EventArgs e)
		{
			GraphViewer.Graph.SatisfyAllItemDemands();
			GraphViewer.AddRemoveElements();
			GraphViewer.PositionNodes();
		}

		private void ClearButton_Click(object sender, EventArgs e)
		{
			GraphViewer.Graph.Nodes.Clear();
			GraphViewer.Elements.Clear();
			GraphViewer.Invalidate();
		}

		private void ItemListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (ItemListBox.SelectedItems.Count == 0)
			{
				AddItemButton.Enabled = false;
			}
			else if (ItemListBox.SelectedItems.Count == 1)
			{
				AddItemButton.Enabled = true;
				AddItemButton.Text = "Add Output";
			}
			else if (ItemListBox.SelectedItems.Count > 1)
			{
				AddItemButton.Enabled = true;
				AddItemButton.Text = "Add Outputs";
			}
		}

		private void AssemblerDisplayCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			GraphViewer.ShowAssemblers = (sender as CheckBox).Checked;
			GraphViewer.UpdateNodes();
		}

		private void SingleAssemblerPerRecipeCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			GraphViewer.Graph.OneAssemblerPerRecipe = (sender as CheckBox).Checked;
			GraphViewer.UpdateNodes();
		}

		private void ExportImageButton_Click(object sender, EventArgs e)
		{
			ImageExportForm form = new ImageExportForm(GraphViewer);			
			form.Show();
		}

		private void AssemblerSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			((Assembler)AssemblerSelectionBox.Items[e.Index]).Enabled = e.NewValue == CheckState.Checked;
			GraphViewer.UpdateNodes();
		}

		private void MinerDisplayCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			GraphViewer.ShowMiners = (sender as CheckBox).Checked;
			GraphViewer.UpdateNodes();
		}

		private void MinerSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			((Miner)MinerSelectionBox.Items[e.Index]).Enabled = e.NewValue == CheckState.Checked;
			GraphViewer.UpdateNodes();
		}

		private void ModuleSelectionBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			((Module)ModuleSelectionBox.Items[e.Index]).Enabled = e.NewValue == CheckState.Checked;
			GraphViewer.UpdateNodes();
		}
	}
}
