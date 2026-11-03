using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// MrBean Visuals Setup — зелёно-чёрный установщик лаунчера.
public static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		if (args.Length > 0 && string.Equals(args[0], "/uninstall", StringComparison.OrdinalIgnoreCase))
		{
			bool quiet = args.Length > 1 && string.Equals(args[1], "/silent", StringComparison.OrdinalIgnoreCase);
			Uninstaller.Run(quiet);
			return;
		}
		string dir = null;
		bool silent = false;
		foreach (string a in args)
		{
			if (a.StartsWith("/dir=", StringComparison.OrdinalIgnoreCase))
			{
				dir = a.Substring(5).Trim('"');
			}
			if (string.Equals(a, "/silent", StringComparison.OrdinalIgnoreCase))
			{
				silent = true;
			}
		}
		if (silent)
		{
			int code = SetupForm.InstallTo(dir ?? SetupForm.DefaultDir(), true, true);
			Environment.Exit(code);
			return;
		}
		Application.Run(new SetupForm());
	}
}

public static class Ui
{
	public static readonly Color Black = Color.FromArgb(10, 12, 10);
	public static readonly Color Card = Color.FromArgb(18, 22, 18);
	public static readonly Color Green = Color.FromArgb(63, 210, 124);
	public static readonly Color Dim = Color.FromArgb(140, 160, 145);
	public static readonly Color Text = Color.FromArgb(232, 244, 235);

	public static Font Black16()
	{
		return new Font("Segoe UI", 16f, FontStyle.Bold);
	}

	public static Font Reg(float s)
	{
		return new Font("Segoe UI", s, FontStyle.Regular);
	}
}

public sealed class SetupForm : Form
{
	private readonly TextBox dirBox = new TextBox();
	private readonly CheckBox desktopChk = new CheckBox();
	private readonly CheckBox startMenuChk = new CheckBox();
	private readonly ProgressBar bar = new ProgressBar();
	private readonly Label statusLbl = new Label();
	private Button installBtn;
	private bool updating;

	public SetupForm()
	{
		Text = "MrBean Visuals — установка";
		ClientSize = new Size(560, 420);
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		BackColor = Ui.Black;
		try
		{
			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}

		var title = new Label
		{
			Text = "MrBean Visuals",
			Font = Ui.Black16(),
			ForeColor = Ui.Text,
			BackColor = Ui.Black,
			Bounds = new Rectangle(28, 24, 400, 34)
		};
		var sub = new Label
		{
			Text = "Лаунчер Minecraft 1.21.11 + MrBean Visuals",
			Font = Ui.Reg(9.5f),
			ForeColor = Ui.Dim,
			BackColor = Ui.Black,
			Bounds = new Rectangle(30, 58, 460, 20)
		};
		var dirLbl = new Label
		{
			Text = "Папка установки",
			Font = Ui.Reg(9.5f),
			ForeColor = Ui.Text,
			BackColor = Ui.Black,
			Bounds = new Rectangle(30, 106, 200, 20)
		};
		string def = DefaultDir();
		updating = File.Exists(Path.Combine(def, "MrBeanLauncher.exe"));
		dirBox.Bounds = new Rectangle(30, 130, 410, 26);
		dirBox.Font = Ui.Reg(9.5f);
		dirBox.Text = def;
		var browseBtn = new Button
		{
			Text = "Обзор…",
			FlatStyle = FlatStyle.Flat,
			BackColor = Ui.Card,
			ForeColor = Ui.Green,
			Font = Ui.Reg(9.5f),
			Bounds = new Rectangle(450, 129, 80, 28)
		};
		browseBtn.FlatAppearance.BorderSize = 0;
		browseBtn.Click += delegate
		{
			using (var fd = new FolderBrowserDialog())
			{
				if (Directory.Exists(dirBox.Text))
				{
					fd.SelectedPath = dirBox.Text;
				}
				if (fd.ShowDialog(this) == DialogResult.OK)
				{
					dirBox.Text = fd.SelectedPath;
				}
			}
		};

		desktopChk.Text = "Ярлык на рабочем столе";
		desktopChk.ForeColor = Ui.Text;
		desktopChk.BackColor = Ui.Black;
		desktopChk.Checked = true;
		desktopChk.Font = Ui.Reg(9.5f);
		desktopChk.Bounds = new Rectangle(30, 176, 240, 24);
		startMenuChk.Text = "Ярлык в меню «Пуск»";
		startMenuChk.ForeColor = Ui.Text;
		startMenuChk.BackColor = Ui.Black;
		startMenuChk.Checked = true;
		startMenuChk.Font = Ui.Reg(9.5f);
		startMenuChk.Bounds = new Rectangle(290, 176, 220, 24);

		var note = new Label
		{
			Text = updating
				? "Найдена существующая установка — настройки (launcher.json, кэш, моды) будут сохранены."
				: "Установка с нуля. Все файлы лаунчера лежат в выбранной папке.",
			Font = Ui.Reg(9f),
			ForeColor = Ui.Green,
			BackColor = Ui.Black,
			Bounds = new Rectangle(30, 214, 500, 34)
		};

		bar.Bounds = new Rectangle(30, 268, 500, 10);
		bar.Maximum = 1000;
		bar.Style = ProgressBarStyle.Continuous;
		bar.ForeColor = Ui.Green;

		statusLbl.Text = string.Empty;
		statusLbl.ForeColor = Ui.Dim;
		statusLbl.BackColor = Ui.Black;
		statusLbl.Font = Ui.Reg(9f);
		statusLbl.Bounds = new Rectangle(30, 286, 500, 20);

		installBtn = new Button
		{
			Text = updating ? "Обновить" : "Установить",
			FlatStyle = FlatStyle.Flat,
			BackColor = Ui.Green,
			ForeColor = Color.FromArgb(6, 14, 9),
			Font = new Font("Segoe UI", 11f, FontStyle.Bold),
			Bounds = new Rectangle(30, 330, 500, 44)
		};
		installBtn.FlatAppearance.BorderSize = 0;
		installBtn.Click += delegate { Install(); };

		Controls.Add(title);
		Controls.Add(sub);
		Controls.Add(dirLbl);
		Controls.Add(dirBox);
		Controls.Add(browseBtn);
		Controls.Add(desktopChk);
		Controls.Add(startMenuChk);
		Controls.Add(note);
		Controls.Add(bar);
		Controls.Add(statusLbl);
		Controls.Add(installBtn);
		Paint += delegate (object s, PaintEventArgs e)
		{
			using (var b = new SolidBrush(Ui.Card))
			{
				e.Graphics.FillRectangle(b, 0, 0, ClientSize.Width, 88);
			}
			using (var pen = new Pen(Ui.Green, 2f))
			{
				e.Graphics.DrawLine(pen, 0, 88, ClientSize.Width, 88);
			}
		};
	}

	private void SetStatus(string text, int permille)
	{
		statusLbl.Text = text;
		bar.Value = Math.Min(permille, 1000);
		Application.DoEvents();
	}

	private void Install()
	{
		installBtn.Enabled = false;
		try
		{
			string root = dirBox.Text.Trim().Trim('"');
			if (string.IsNullOrEmpty(root))
			{
				root = DefaultDir();
			}
			SetStatus("Распаковка файлов…", 60);
			InstallTo(root, desktopChk.Checked, startMenuChk.Checked);
			bar.Value = 1000;
			statusLbl.Text = "Готово.";
			MessageBox.Show(this,
				updating ? "MrBean Visuals обновлён. Ваши настройки сохранены." : "MrBean Visuals установлен.",
				"MrBean Visuals", MessageBoxButtons.OK, MessageBoxIcon.Information);
			Close();
		}
		catch (Exception ex)
		{
			installBtn.Enabled = true;
			MessageBox.Show(this, "Ошибка установки: " + ex.Message, "MrBean Visuals", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	public static string DefaultDir()
	{
		string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return Path.Combine(local, "Programs", "MrBean");
	}

	public static int InstallTo(string root, bool desktop, bool startMenu)
	{
		try
		{
			Directory.CreateDirectory(root);
			ExtractPayload(root);
			string exe = Path.Combine(root, "MrBeanLauncher.exe");
			if (desktop)
			{
				string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				Shortcut(Path.Combine(desktopDir, "MrBean Launcher.lnk"), exe, root);
			}
			if (startMenu)
			{
				string start = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
				Directory.CreateDirectory(start);
				Shortcut(Path.Combine(start, "MrBean Launcher.lnk"), exe, root);
			}
			WriteUninstallKey(root, exe);
			return 0;
		}
		catch (Exception ex)
		{
			try
			{
				File.AppendAllText(Path.Combine(root, "setup-error.log"), DateTime.Now + " " + ex + Environment.NewLine);
			}
			catch
			{
			}

			return 1;
		}
	}

	private static void ExtractPayload(string root)
	{
		using (var res = typeof(SetupForm).Assembly.GetManifestResourceStream("Setup.payload.zip"))
		{
			if (res == null)
			{
				throw new Exception("внутри установщика нет архива с файлами (Setup.payload.zip)");
			}
			using (var ms = new MemoryStream())
			{
				res.CopyTo(ms);
				ms.Position = 0L;
				using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
				{
					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						if (string.IsNullOrEmpty(entry.Name))
						{
							continue;
						}
						string target = Path.Combine(root, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
						if (IsUserFile(entry.FullName) && File.Exists(target))
						{
							continue;
						}
						Directory.CreateDirectory(Path.GetDirectoryName(target));
						bool locked = false;
						for (int attempt = 0; attempt < 3 && !locked; attempt++)
						{
							try
							{
								using (Stream src = entry.Open())
								using (var dst = new FileStream(target, FileMode.Create, FileAccess.Write))
								{
									src.CopyTo(dst);
								}
								locked = true;
							}
							catch (IOException)
							{
								Thread.Sleep(500);
							}
						}
						if (!locked)
						{
							throw new Exception("не удалось перезаписать " + entry.FullName + " — закройте лаунчер и игру");
						}
					}
				}
			}
		}
	}

	private static bool IsUserFile(string name)
	{
		string n = name.Replace('\\', '/');
		if (n.Equals("launcher.json", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return n.StartsWith("cache/", StringComparison.OrdinalIgnoreCase)
			|| n.StartsWith("versions/", StringComparison.OrdinalIgnoreCase)
			|| n.StartsWith("mods/", StringComparison.OrdinalIgnoreCase)
			|| n.StartsWith("runtime/", StringComparison.OrdinalIgnoreCase);
	}

	private static void WriteUninstallKey(string root, string exe)
	{
		using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
			@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MrBeanVisuals"))
		{
			key.SetValue("DisplayName", "MrBean Visuals");
			key.SetValue("DisplayIcon", exe);
			key.SetValue("DisplayVersion", FileVersionInfo.GetVersionInfo(exe).ProductVersion ?? "3.5.0");
			key.SetValue("Publisher", "MrBean");
			key.SetValue("InstallLocation", root);
			key.SetValue("UninstallString", "\"" + Application.ExecutablePath + "\" /uninstall");
			key.SetValue("NoModify", 1, RegistryValueKind.DWord);
			key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
		}
	}

	private static void Shortcut(string path, string target, string workDir)
	{
		dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
		dynamic sc = shell.CreateShortcut(path);
		sc.TargetPath = target;
		sc.WorkingDirectory = workDir;
		sc.IconLocation = target + ",0";
		sc.Description = "MrBean Launcher";
		sc.Save();
	}
}

public static class Uninstaller
{
	public static void Run(bool quiet)
	{
		string root = null;
		using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
			@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MrBeanVisuals"))
		{
			if (key != null)
			{
				root = key.GetValue("InstallLocation") as string;
			}
		}
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
		{
			MessageBox.Show("Папка установки не найдена.", "MrBean Visuals");
			return;
		}

		bool keepSettings;
		if (quiet)
		{
			keepSettings = true;
		}
		else
		{
			var result = MessageBox.Show(
				"Удалить MrBean Visuals?\n\n«Да» — удалить программу и все файлы, включая настройки." +
				"\n«Нет» — удалить программу, но оставить launcher.json, кэш и игровые файлы." +
				"\n«Отмена» — ничего не делать.",
				"MrBean Visuals", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if (result != DialogResult.Yes && result != DialogResult.No)
			{
				return;
			}

			keepSettings = result == DialogResult.No;
		}
		try
		{
			foreach (string shortcut in new string[]
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MrBean Launcher.lnk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "MrBean Launcher.lnk")
			})
			{
				TryDelete(shortcut);
			}

			if (keepSettings)
			{
				TryDelete(Path.Combine(root, "MrBeanLauncher.exe"));
				TryDelete(Path.Combine(root, "MrBeanLauncher.exe.config"));
				TryDelete(Path.Combine(root, "MrBeanUpdater.exe"));
				TryDeleteDir(Path.Combine(root, "bg"));
			}
			else
			{
				Directory.Delete(root, true);
			}

			Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MrBeanVisuals", false);
			MessageBox.Show("Готово.", "MrBean Visuals");
		}
		catch (Exception ex)
		{
			MessageBox.Show("Ошибка удаления: " + ex.Message, "MrBean Visuals");
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static void TryDeleteDir(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
		catch
		{
		}
	}
}
