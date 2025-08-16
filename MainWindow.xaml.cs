using Microsoft.Win32;
using Microsoft.VisualBasic; // dla Interaction.InputBox
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VUYAexe1
{
    public partial class MainWindow : Window
    {
        private readonly DataTable _table = new DataTable("Items");
        private string? _selectedColumnName;

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show($"Wystąpił nieobsłużony wyjątek:\n{e.Exception.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            InitTable();
            BindTable();
            RebuildGridColumns();
            ApplyFilter();
            UpdateSelectionInfo();

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

        private void RebuildGridColumns()
        {
            GridItems.Columns.Clear();

            foreach (DataColumn col in _table.Columns)
            {
                var column = new DataGridTextColumn
                {
                    Header = col.ColumnName,
                    Binding = new System.Windows.Data.Binding(col.ColumnName),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };


                GridItems.Columns.Add(column);
            }
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

        // Filtr
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
                if (CbFilter.Items.Count > 0) CbFilter.SelectedIndex = 0; // "Wszystkie"
            }
        }

        private void CbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (CbFilter.Items.Count > 0) (CbFilter.Items[0] as ComboBoxItem)!.IsSelected = true; // "Wszystkie"
            ApplyFilter();
        }

        // Wiersze
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
            if (GridItems.SelectedItem is DataRowView drv) drv.Row.Delete();
        }


        // Sort
        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            var cols = _table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToHashSet();
            var order = new List<string>();
            if (cols.Contains("Kategoria")) order.Add("[Kategoria] ASC");
            if (cols.Contains("Nazwa")) order.Add("[Nazwa] ASC");
            _table.DefaultView.Sort = string.Join(", ", order);
        }

        // Podgląd / Zasoby
        private void GridItems_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelectionInfo();

        private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            if (GridItems.SelectedItem is not DataRowView drv) return;

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
            if (GridItems.SelectedItem is not DataRowView drv) return;
            if (_table.Columns.Contains("ŚcieżkaObrazu")) drv.Row["ŚcieżkaObrazu"] = "";
            ImgPreview.Source = null;
        }



        private void EnsureColumn(string name)
        {
            if (!_table.Columns.Contains(name))
            {
                _table.Columns.Add(name, typeof(string));
                BindTable();
            }
        }

        // Zapis / Odczyt JSON
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

            string json;
            try
            {
                json = File.ReadAllText(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się odczytać pliku:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TableDto dto;
            try
            {
                dto = JsonSerializer.Deserialize<TableDto>(json) ?? new TableDto();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nieprawidłowy JSON:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _table.Clear();
            _table.Columns.Clear();

            foreach (var col in dto.Columns ?? Enumerable.Empty<string>())
                _table.Columns.Add(col, typeof(string));

            foreach (var rowDict in dto.Rows ?? Enumerable.Empty<Dictionary<string, string?>>())
            {
                var row = _table.NewRow();
                foreach (DataColumn c in _table.Columns)
                {
                    if (rowDict != null && rowDict.TryGetValue(c.ColumnName, out var val))
                        row[c.ColumnName] = val ?? "";
                    else
                        row[c.ColumnName] = "";
                }
                _table.Rows.Add(row);
            }

            BindTable();
            RebuildGridColumns();
            ApplyFilter();
            UpdateSelectionInfo();
        }

        private void ClearSortIfInvalid()
        {
            var sort = _table.DefaultView.Sort;
            if (string.IsNullOrWhiteSpace(sort)) return;

            var validCols = new HashSet<string>(_table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            var parts = sort.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

            var filtered = parts.Where(p =>
            {
                var space = p.IndexOf(' ');
                var col = space > 0 ? p[..space] : p;
                col = col.Trim('[', ']');
                return validCols.Contains(col);
            }).ToList();

            _table.DefaultView.Sort = string.Join(", ", filtered);
        }
