using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLua;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

namespace Foreman
{
	static class DataCache
	{
		private static string factorioPath;
		private static List<string> mods;

		public static Dictionary<String, Item> Items = new Dictionary<String, Item>();
		public static Dictionary<String, Recipe> Recipes = new Dictionary<String, Recipe>();
		public static Dictionary<String, Assembler> Assemblers = new Dictionary<string, Assembler>();
		public static Dictionary<String, Miner> Miners = new Dictionary<string, Miner>();
		public static Dictionary<String, Resource> Resources = new Dictionary<string, Resource>();
		public static Dictionary<String, Module> Modules = new Dictionary<string, Module>();

		private const float defaultRecipeTime = 0.5f;
		private static Dictionary<Bitmap, Color> colourCache = new Dictionary<Bitmap, Color>();
		public static Bitmap UnknownIcon;
		public static Dictionary<String, String> KnownRecipeNames = new Dictionary<string, string>();
		public static Dictionary<String, Dictionary<String, String>> LocaleFiles = new Dictionary<string, Dictionary<string, string>>();

		public static void LoadRecipes(string factorioPath, List<string> mods)
		{
			DataCache.factorioPath = factorioPath;
			DataCache.mods = mods;

			using (Lua lua = new Lua())
			{
				try
				{
					AddLuaPackagePath(lua, Path.Combine(factorioPath, "data", "core", "lualib"));
					lua.DoFile(Path.Combine(factorioPath, "data", "core", "lualib", "dataloader.lua"));

					Assembly assembly = Assembly.GetExecutingAssembly();
					Stream stream = assembly.GetManifestResourceStream("Foreman.lua-compat.lua");
					StreamReader rd = new StreamReader(stream);
					lua.DoString(rd.ReadToEnd());

					AddLuaPackagePath(lua, Path.Combine(factorioPath, "data", "base"));
					lua.DoFile(Path.Combine(factorioPath, "data", "base", "data.lua"));

					foreach(string modDirectory in mods)
					{
						AddLuaPackagePath(lua, Path.Combine(modDirectory));
						lua.DoFile(Path.Combine(modDirectory, "data.lua"));
					}
				}
				catch (NLua.Exceptions.LuaScriptException e)
				{
					Console.Out.WriteLine("error: "+e.ToString());
					MessageBox.Show("Error loading factorio game data: "+e.ToString());
				}

				foreach (String type in new List<String> { "item", "fluid", "capsule", "module", "ammo", "gun", "armor", "blueprint", "deconstruction-item" })
				{
					InterpretItems(lua, type);
				}

				LuaTable recipeTable = lua.GetTable("data.raw")["recipe"] as LuaTable;
				if (recipeTable != null)
				{
					var recipeEnumerator = recipeTable.GetEnumerator();
					while (recipeEnumerator.MoveNext())
					{
						InterpretLuaRecipe(recipeEnumerator.Key as String, recipeEnumerator.Value as LuaTable);
					}
				}

				LuaTable assemblerTable = lua.GetTable("data.raw")["assembling-machine"] as LuaTable;
				if (assemblerTable != null)
				{
					var assemblerEnumerator = assemblerTable.GetEnumerator();
					while (assemblerEnumerator.MoveNext())
					{
						InterpretAssemblingMachine(assemblerEnumerator.Key as String, assemblerEnumerator.Value as LuaTable);
					}
				}

				LuaTable furnaceTable = lua.GetTable("data.raw")["furnace"] as LuaTable;
				if (furnaceTable != null)
				{
					var furnaceEnumerator = furnaceTable.GetEnumerator();
					while (furnaceEnumerator.MoveNext())
					{
						InterpretFurnace(furnaceEnumerator.Key as String, furnaceEnumerator.Value as LuaTable);
					}
				}

				LuaTable minerTable = lua.GetTable("data.raw")["mining-drill"] as LuaTable;
				if (minerTable != null)
				{
					var minerEnumerator = minerTable.GetEnumerator();
					while (minerEnumerator.MoveNext())
					{
						InterpretMiner(minerEnumerator.Key as String, minerEnumerator.Value as LuaTable);
					}
				}

				LuaTable resourceTable = lua.GetTable("data.raw")["resource"] as LuaTable;
				if (resourceTable != null)
				{
					var resourceEnumerator = resourceTable.GetEnumerator();
					while (resourceEnumerator.MoveNext())
					{
						InterpretResource(resourceEnumerator.Key as String, resourceEnumerator.Value as LuaTable);
					}
				}

				LuaTable moduleTable = lua.GetTable("data.raw")["module"] as LuaTable;
				if (moduleTable != null)
				{
					foreach (String moduleName in moduleTable.Keys)
					{
						InterpretModule(moduleName, moduleTable[moduleName] as LuaTable);
					}
				}

				UnknownIcon = LoadImage("UnknownIcon.png");

				LoadLocaleFiles();

				LoadItemNames("item-name");
				LoadItemNames("fluid-name");
				LoadItemNames("entity-name");
				LoadItemNames("equipment-name");
				LoadRecipeNames();
				LoadEntityNames();
				LoadModuleNames();
			}
		}

		private static void AddLuaPackagePath(Lua lua, string dir)
		{
			string luaCommand = String.Format("package.path = package.path .. ';{0}{1}?.lua'", dir, Path.DirectorySeparatorChar);
			luaCommand = luaCommand.Replace("\\", "\\\\");
			lua.DoString(luaCommand);
		}

		private static IEnumerable<String> getAllModDirs()
		{
			List<String> dirs = new List<String>();
			dirs.Add(Path.Combine(factorioPath, "data", "core"));
			dirs.Add(Path.Combine(factorioPath, "data", "base"));
			dirs.AddRange(mods);
			return dirs;
		}

		private static void InterpretItems(Lua lua, String typeName)
		{
			LuaTable itemTable = lua.GetTable("data.raw")[typeName] as LuaTable;

			if (itemTable != null)
			{
				var enumerator = itemTable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					InterpretLuaItem(enumerator.Key as String, enumerator.Value as LuaTable);
				}
			}
		}

		private static void LoadLocaleFiles(String locale = "en")
		{
			foreach (String dir in getAllModDirs())
			{
				String localeDir = Path.Combine(dir, "locale", locale);
				if (Directory.Exists(localeDir))
				{
					foreach (String file in Directory.GetFiles(localeDir, "*.cfg"))
					{
						try
						{
							using (StreamReader fStream = new StreamReader(file))
							{
								string currentIniSection = "none";

								while (!fStream.EndOfStream)
								{
									String line = fStream.ReadLine();
									if (line.StartsWith("[") && line.EndsWith("]"))
									{
										currentIniSection = line.Trim('[', ']');
									}
									else
									{
										if (!LocaleFiles.ContainsKey(currentIniSection))
										{
											LocaleFiles.Add(currentIniSection, new Dictionary<string, string>());
										}
										String[] split = line.Split('=');
										if (split.Count() == 2)
										{
											LocaleFiles[currentIniSection][split[0]] = split[1];
										}
									}
								}
							}
						}
						catch (Exception e)
						{
							MessageBox.Show("Failed to load locale file "+file+": "+e.ToString());
						}
					}
				}
			}
		}

		private static void LoadItemNames(String category)
		{
			foreach (var kvp in LocaleFiles[category])
			{
				if (Items.ContainsKey(kvp.Key))
				{
					Items[kvp.Key].FriendlyName = kvp.Value;
				}
			}
		}

		private static void LoadRecipeNames(String locale = "en")
		{
			foreach (var kvp in LocaleFiles["recipe-name"])
			{
				KnownRecipeNames[kvp.Key] = kvp.Value;
			}
		}

		private static void LoadEntityNames(String locale = "en")
		{
			foreach (var kvp in LocaleFiles["entity-name"])
			{
				if (Assemblers.ContainsKey(kvp.Key))
				{
					Assemblers[kvp.Key].FriendlyName = kvp.Value;
				}
				if (Miners.ContainsKey(kvp.Key))
				{
					Miners[kvp.Key].FriendlyName = kvp.Value;
				}
			}
		}

		private static void LoadModuleNames(String locale = "en")
		{
			foreach (var kvp in LocaleFiles["item-name"])
			{
				if (Modules.ContainsKey(kvp.Key))
				{
					Modules[kvp.Key].FriendlyName = kvp.Value;
				}
			}
		}

		private static Bitmap LoadImage(String fileName)
		{
			string fullPath;
			if (File.Exists(fileName))
			{
				fullPath = fileName;
			}
			else
			{
				string[] splitPath = fileName.Split('/');
				splitPath[0] = splitPath[0].Trim('_');
				fullPath = getAllModDirs().FirstOrDefault(d => Path.GetFileName(d) == splitPath[0]);

				if (!String.IsNullOrEmpty(fullPath))
				{
					for (int i = 1; i < splitPath.Count(); i++ ) //Skip the first split section because it's the same as the end of the path already
					{
						fullPath = Path.Combine(fullPath, splitPath[i]);
					}
				}
			}

			try
			{
				Bitmap image = new Bitmap(fullPath);
				return image;
			}
			catch (Exception e)
			{
				return null;
			}
		}

		public static Color IconAverageColour(Bitmap icon)
		{
			if (icon == null)
			{
				return Color.LightGray;
			}

			Color result;
			if (colourCache.ContainsKey(icon))
			{
				result = colourCache[icon];
			}
			else
			{
				using (Bitmap pixel = new Bitmap(1, 1))
				using (Graphics g = Graphics.FromImage(pixel))
				{
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
					g.DrawImage(icon, new Rectangle(0, 0, 1, 1)); //Scale the icon down to a 1-pixel image, which does the averaging for us
					result = pixel.GetPixel(0, 0);
				}
				result = Color.FromArgb(255, result.R + (255 - result.R) / 2, result.G + (255 - result.G) / 2, result.B + (255 - result.B) / 2);
				colourCache.Add(icon, result);
			}

			return result;
		}

		private static void InterpretLuaItem(String name, LuaTable values)
		{
			Item newItem = new Item(name);
			newItem.Icon = LoadImage(values["icon"] as String);

			if (!Items.ContainsKey(name))
			{
				Items.Add(name, newItem);
			}
		}

		//This is only if a recipe references an item that isn't in the item prototypes (which shouldn't really happen)
		private static Item LoadItemFromRecipe(String itemName)
		{
			Item newItem;
			if (!Items.ContainsKey(itemName))
			{
				Items.Add(itemName, newItem = new Item(itemName));
			}
			else
			{
				newItem = Items[itemName];
			}
			return newItem;
		}

		private static void InterpretLuaRecipe(String name, LuaTable values)
		{
			float time = Convert.ToSingle(values["energy_required"]);
			Dictionary<Item, float> ingredients = extractIngredientsFromLuaRecipe(values);
			Dictionary<Item, float> results = extractResultsFromLuaRecipe(values);

			Recipe newRecipe = new Recipe(name, time == 0.0f ? defaultRecipeTime : time, ingredients, results);

			newRecipe.Category = values["category"] as String ?? "crafting";

			String iconFile = values["icon"] as String;
			if (iconFile != null)
			{
				Bitmap icon = LoadImage(iconFile);
				newRecipe.Icon = icon;
			}

			foreach (Item result in results.Keys)
			{
				result.Recipes.Add(newRecipe);
			}
			Recipes.Add(newRecipe.Name, newRecipe);
		}

		private static void InterpretAssemblingMachine(String name, LuaTable values)
		{
			Assembler newAssembler = new Assembler(name);

			newAssembler.Icon = LoadImage(values["icon"] as String);
			newAssembler.MaxIngredients = Convert.ToInt32(values["ingredient_count"]);
			newAssembler.ModuleSlots = Convert.ToInt32(values["module_slots"]);
			if (newAssembler.ModuleSlots == 0) newAssembler.ModuleSlots = 2;
			newAssembler.Speed = Convert.ToSingle(values["crafting_speed"]);

			LuaTable effects = values["allowed_effects"] as LuaTable;
			if (effects != null)
			{
				foreach (String effect in effects.Values)
				{
					newAssembler.AllowedEffects.Add(effect);
				}
			}
			foreach (String category in (values["crafting_categories"] as LuaTable).Values)
			{
				newAssembler.Categories.Add(category);
			}

			Assemblers.Add(newAssembler.Name, newAssembler);
		}

		private static void InterpretFurnace(String name, LuaTable values)
		{
			Assembler newFurnace = new Assembler(name);

			newFurnace.Icon = LoadImage(values["icon"] as String);
			newFurnace.MaxIngredients = 1;
			newFurnace.ModuleSlots = Convert.ToInt32(values["module_slots"]);
			newFurnace.Speed = Convert.ToSingle(values["crafting_speed"]);

			foreach (String category in (values["crafting_categories"] as LuaTable).Values)
			{
				newFurnace.Categories.Add(category);
			}

			Assemblers.Add(newFurnace.Name, newFurnace);
		}

		private static void InterpretMiner(String name, LuaTable values)
		{
			Miner newMiner = new Miner(name);

			newMiner.Icon = LoadImage(values["icon"] as String);
			newMiner.MiningPower = Convert.ToSingle(values["mining_power"]);
			newMiner.Speed = Convert.ToSingle(values["mining_speed"]);
			newMiner.ModuleSlots = Convert.ToInt32(values["module_slots"]);

			LuaTable categories = values["resource_categories"] as LuaTable;
			if (categories != null)
			{
				foreach (String category in categories.Values)
				{
					newMiner.ResourceCategories.Add(category);
				}
			}

			Miners.Add(name, newMiner);
		}

		private static void InterpretResource(String name, LuaTable values)
		{
			Resource newResource = new Resource(name);
			newResource.Category = values["category"] as String;
			if (String.IsNullOrEmpty(newResource.Category))
			{
				newResource.Category = "basic-solid";
			}
			LuaTable minableTable = values["minable"] as LuaTable;
			newResource.Hardness = Convert.ToSingle(minableTable["hardness"]);
			newResource.Time = Convert.ToSingle(minableTable["mining_time"]);

			if (minableTable["result"] != null)
			{
				newResource.result = minableTable["result"] as String;
			}
			else
			{
				newResource.result = ((minableTable["results"] as LuaTable)[1] as LuaTable)["name"] as String;
			}

			Resources.Add(name, newResource);
		}

		private static void InterpretModule(String name, LuaTable values)
		{
			float speedBonus = 0f;

			LuaTable effectTable = values["effect"] as LuaTable;
			LuaTable speed = effectTable["speed"] as LuaTable;
			if (speed != null)
			{
				speedBonus = Convert.ToSingle(speed["bonus"]);
			}

			if (speed == null || speedBonus <= 0)
			{
				return;
			}

			Modules.Add(name, new Module(name, speedBonus));
		}

		private static Dictionary<Item, float> extractResultsFromLuaRecipe(LuaTable values)
		{
			Dictionary<Item, float> results = new Dictionary<Item, float>();
			if (values["result"] is String)
			{
				float resultCount = Convert.ToSingle(values["result_count"]);
				if (resultCount == 0f)
				{
					resultCount = 1f;
				}
				results.Add(LoadItemFromRecipe(values["result"] as String), resultCount);
			}
			else
			{
				var resultEnumerator = (values["results"] as LuaTable).GetEnumerator();
				while (resultEnumerator.MoveNext())
				{
					LuaTable resultTable = resultEnumerator.Value as LuaTable;
					results.Add(LoadItemFromRecipe(resultTable["name"] as String), Convert.ToSingle(resultTable["amount"]));
				}
			}

			return results;
		}

		private static Dictionary<Item, float> extractIngredientsFromLuaRecipe(LuaTable values)
		{
			Dictionary<Item, float> ingredients = new Dictionary<Item, float>();
			var ingredientEnumerator = (values["ingredients"] as LuaTable).GetEnumerator();
			while (ingredientEnumerator.MoveNext())
			{
				LuaTable ingredientTable = ingredientEnumerator.Value as LuaTable;
				String name;
				float amount;
				if (ingredientTable["name"] != null)
				{
					name = ingredientTable["name"] as String;
				}
				else
				{
					name = ingredientTable[1] as String;
				}
				if (ingredientTable["amount"] != null)
				{
					amount = Convert.ToSingle(ingredientTable["amount"]);
				}
				else
				{
					amount = Convert.ToSingle(ingredientTable[2]);
				}
				ingredients.Add(LoadItemFromRecipe(name), amount);
			}

			return ingredients;
		}

	}
}