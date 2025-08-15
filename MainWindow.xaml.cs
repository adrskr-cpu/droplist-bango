using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VUYAexe1
{
    public partial class MainWindow : Window
    {
        private readonly DataTable _table = new DataTable("Items");
        private readonly MediaPlayer _player = new MediaPlayer();

        public MainWindow()
        {
            InitializeComponent();
            InitTable();
            BindTable();
            ApplyFilter();
            UpdateSelectionInfo();

            _player.MediaEnded += (_, __) => _player.Stop();
        }

        private void InitTable()
        {
            _table.Clear();
            _table.Columns.Clear();

            _table.Columns.Add("Nazwa", typeof(string));
            _table.Columns.Add("Kategoria", typeof(string));
            _table.Columns.Add("Opis", typeof(string));
            _table.Columns.Add("Wartość", typeof(string));
            _table.Columns.Add("ŚcieżkaObrazu", typeof(string));
            _table.Columns.Add("ŚcieżkaDźwięku", typeof(string));

            var r = _table.NewRow();
            r["Nazwa"] = "Miecz żelazny";
            r["Kategoria"] = "Broń";
            r["Opis"] = "Podstawowy miecz.";
            r["Wartość"] = "100";
            _table.Rows.Add(r);
        }

        private void BindTable()
        {
            GridItems.ItemsSource = null;
            GridItems.ItemsSource = _table.DefaultView;
        }

        private void UpdateSelectionInfo()
        {
            if (GridItems.SelectedItem is DataRowView drv)
            {
                var name = SafeGet(drv, "Nazwa");
                var cat = SafeGet(drv, "Kategoria");
                var val = SafeGet(drv, "Wartość");
                LblSelectedInfo.Text = $"Nazwa: {name} | Kategoria: {cat} | Wartość: {val}";

                var imgPath = SafeGet(drv, "ŚcieżkaObrazu");
                if (!string.IsNullOrWhiteSpace(imgPath) && File.Exists(imgPath))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(imgPath, UriKind.Absolute);
                        bmp.EndInit();
                        ImgPreview.Source = bmp;
                    }
                    catch
                    {
                        ImgPreview.Source = null;
                    }
                }
                else
                {
                    ImgPreview.Source = null;
                }
            }
            else
            {
                LblSelectedInfo.Text = "Brak zaznaczonego wiersza";
                ImgPreview.Source = null;
            }
        }

        private static string SafeGet(DataRowView? drv, string column)
        {
            if (drv?.DataView?.Table?.Columns.Contains(column) == true)
                return drv.Row[column]?.ToString() ?? string.Empty;

            return string.Empty;
        }

        // ---------------- Filtr ----------------

        private void ApplyFilter()
        {
            var selected = ((CbFilter.SelectedItem as ComboBoxItem)?.Content as string) ?? "";
            if (!_table.Columns.Contains("Kategoria") || string.IsNullOrWhiteSpace(selected) || selected == "Wszystkie")
            {
                _table.DefaultView.RowFilter = "";
                return;
            }

            var value = selected.Replace("'", "''");
            _table.DefaultView.RowFilter = $"[Kategoria] = '{value}'";
        }

        private void ClearFilterIfInvalid()
        {
            if (!_table.Columns.Contains("Kategoria"))
            {
                _table.DefaultView.RowFilter = "";
                if (CbFilter.Items.Count > 0)
                    CbFilter.SelectedIndex = 0; // "Wszystkie"
            }
        }

        private void CbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (CbFilter.Items.Count > 0)
                (CbFilter.Items[0] as ComboBoxItem)!.IsSelected = true; // "Wszystkie"
            ApplyFilter();
        }

        // ---------------- Wiersze ----------------

        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            var r = _table.NewRow();

            if (_table.Columns.Contains("Kategoria"))
            {
                var selected = (CbFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                r["Kategoria"] = !string.IsNullOrWhiteSpace(selected) && selected != "Wszystkie" ? selected : "Inne";
            }

            _table.Rows.Add(r);

            var view = _table.DefaultView;
            if (view.Count > 0)
            {
                GridItems.SelectedIndex = view.Count - 1;
                GridItems.ScrollIntoView(view[view.Count - 1]);
            }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (GridItems.SelectedItem is DataRowView drv)
                drv.Row.Delete();
        }

        // ---------------- Kolumny ----------------

        private void BtnAddColumn_Click(object sender, RoutedEventArgs e)
        {
            var name = TbNewColumnName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Podaj nazwę nowej kolumny.", "Kolumna", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_table.Columns.Contains(name))
            {
                MessageBox.Show("Taka kolumna już istnieje.", "Kolumna", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _table.Columns.Add(name, typeof(string));
            TbNewColumnName.Clear();
            BindTable();

            var col = GridItems.Columns.FirstOrDefault(c => string.Equals(c.Header?.ToString(), name, StringComparison.Ordinal));
            if (col != null)
            {
                GridItems.CurrentColumn = col;
                if (GridItems.Items.Count > 0)
                    GridItems.CurrentCell = new DataGridCellInfo(GridItems.Items[0], col);
            }
        }

        private void BtnRenameColumn_Click(object sender, RoutedEventArgs e)
        {
            // Bezpieczne pobranie kolumny
            var current = GridItems?.CurrentColumn;
            if (current == null || current.Header is not string oldName || string.IsNullOrWhiteSpace(oldName))
            {
                MessageBox.Show("Zaznacz kolumnę do zmiany nazwy (kliknij jej nagłówek).",
                                "Kolumna", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Bezpieczne pobranie nowej nazwy (zgodnie z XAML: TbRenameColumnName)
            var newName = TbRenameColumnName?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Podaj nową nazwę kolumny.",
                                "Kolumna", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Nazwa identyczna
            if (string.Equals(oldName, newName, StringComparison.Ordinal))
                return;

            // Sprawdzenie duplikatu
            if (_table?.Columns.Contains(newName) == true)
            {
                MessageBox.Show("Kolumna o takiej nazwie już istnieje.",
                                "Kolumna", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Null-safe zmiana nazwy
                if (_table is not null)
                {
                    var columns = _table.Columns;
                    if (!string.IsNullOrEmpty(oldName) && columns.Contains(oldName))
                    {
                        var col = columns[oldName];
                        if (col != null)
                            col.ColumnName = newName!; // w tym miejscu mamy gwarancję, że newName nie jest null
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się zmienić nazwy kolumny:\n{ex.Message}",
                                "Kolumna", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TbRenameColumnName?.Clear();
            BindTable();

            // Ustaw fokus na nową kolumnę (jeśli istnieje)
            var newCol = GridItems?.Columns
                                  .FirstOrDefault(c => string.Equals(c.Header?.ToString(), newName, StringComparison.Ordinal));
            if (newCol != null)
                GridItems!.CurrentColumn = newCol;
        }

        private void BtnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            var current = GridItems?.CurrentColumn;
            if (current?.Header is not string name || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Zaznacz kolumnę do usunięcia (kliknij jej nagłówek).",
                                "Kolumna", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_table?.Columns.Contains(name) != true)
                return;

            _table!.Columns.Remove(name);
            ClearFilterIfInvalid();
            ClearSortIfInvalid();
            BindTable();
        }

        private void ClearSortIfInvalid()
        {
            var sort = _table.DefaultView.Sort;
            if (string.IsNullOrWhiteSpace(sort)) return;

            var validCols = new HashSet<string>(_table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            var parts = sort.Split(',').Select(p => p.Trim()).ToList();
            var filtered = parts.Where(p =>
            {
                var space = p.IndexOf(' ');
                var col = space > 0 ? p.Substring(0, space) : p;
                col = col.Trim('[', ']');
                return validCols.Contains(col);
            }).ToList();

            _table.DefaultView.Sort = string.Join(", ", filtered);
        }

        // ---------------- Sort ----------------

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            var cols = _table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToHashSet();
            var order = new List<string>();
            if (cols.Contains("Kategoria")) order.Add("[Kategoria] ASC");
            if (cols.Contains("Nazwa")) order.Add("[Nazwa] ASC");

            _table.DefaultView.Sort = string.Join(", ", order);
        }

        // ---------------- Podgląd / Zasoby ----------------

        private void GridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionInfo();
        }

        private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            if (GridItems.SelectedItem is not DataRowView drv)
                return;

            var dlg = new OpenFileDialog
            {
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Wszystkie pliki|*.*",
                Title = "Wybierz obraz"
            };
            if (dlg.ShowDialog() == true)
            {
                EnsureColumn("ŚcieżkaObrazu");
                drv.Row["ŚcieżkaObrazu"] = dlg.FileName;
                UpdateSelectionInfo();
            }
        }

        private void BtnClearImage_Click(object sender, RoutedEventArgs e)
        {
            if (GridItems.SelectedItem is not DataRowView drv)
                return;

            if (_table.Columns.Contains("ŚcieżkaObrazu"))
                drv.Row["ŚcieżkaObrazu"] = "";
            ImgPreview.Source = null;
        }

        private void BtnChooseSound_Click(object sender, RoutedEventArgs e)
        {
            if (GridItems.SelectedItem is not DataRowView drv)
                return;

            var dlg = new OpenFileDialog
            {
                Filter = "Dźwięki|*.wav;*.mp3;*.ogg;*.flac|Wszystkie pliki|*.*",
                Title = "Wybierz dźwięk"
            };
            if (dlg.ShowDialog() == true)
            {
                EnsureColumn("ŚcieżkaDźwięku");
                drv.Row["ŚcieżkaDźwięku"] = dlg.FileName;
            }
        }

        private void BtnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            if (GridItems.SelectedItem is not DataRowView drv)
                return;

            var path = SafeGet(drv, "ŚcieżkaDźwięku");
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    _player.Open(new Uri(path, UriKind.Absolute));
                    _player.Play();
                }
                catch
                {
                    _player.Stop();
                }
            }
        }

        private void BtnStopSound_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
        }

        private void EnsureColumn(string name)
        {
            if (!_table.Columns.Contains(name))
            {
                _table.Columns.Add(name, typeof(string));
                BindTable();
            }
        }

        // ---------------- Zapis / Odczyt JSON ----------------

        private sealed class TableDto
        {
            public List<string> Columns { get; set; } = new();
            public List<Dictionary<string, string?>> Rows { get; set; } = new();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON|*.json",
                Title = "Zapisz do JSON",
                AddExtension = true,
                DefaultExt = "json"
            };
            if (dlg.ShowDialog() != true) return;

            var dto = new TableDto
            {
                Columns = _table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                Rows = _table.Rows.Cast<DataRow>()
                    .Select(r => _table.Columns.Cast<DataColumn>()
                        .ToDictionary(c => c.ColumnName, c => r[c]?.ToString()))
                    .ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(dto, options));
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON|*.json",
                Title = "Wczytaj z JSON"
            };
            if (dlg.ShowDialog() != true) return;

            var json = File.ReadAllText(dlg.FileName);
            var dto = JsonSerializer.Deserialize<TableDto>(json) ?? new TableDto();

            _table.Clear();
            _table.Columns.Clear();

            foreach (var col in dto.Columns)
                _table.Columns.Add(col, typeof(string));

            foreach (var rowDict in dto.Rows)
            {
                var row = _table.NewRow();
                foreach (DataColumn c in _table.Columns)
                {
                    row[c.ColumnName] = rowDict.TryGetValue(c.ColumnName, out var val) ? val ?? "" : "";
                }
                _table.Rows.Add(row);
            }

            BindTable();
            ApplyFilter();
            UpdateSelectionInfo();
        }
    }
}
