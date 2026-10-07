using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // 1. 集合型態 Dictionary<TKey, TValue> (Key 唯一性，雜湊表 $O(1)$ 查詢)
        private Dictionary<string, int> drinksMenu = new Dictionary<string, int>();
        private Dictionary<string, int> currentOrders = new Dictionary<string, int>();

        private string dineType = "內用";
        private string lastOrderSummary = "";

        public MainWindow()
        {
            InitializeComponent();
            EnsureExportDirectoryExists();
        }

        // 2. 檔案 I/O：Directory 靜態類別檢查與建立資料夾
        private void EnsureExportDirectoryExists()
        {
            string exportFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
            if (!Directory.Exists(exportFolder))
            {
                Directory.CreateDirectory(exportFolder);
            }
        }

        // 3. OpenFileDialog 與 File.ReadAllLines 開啟並讀取 CSV 檔案
        private void btnLoadCsv_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "請選擇菜單 CSV 檔案",
                Filter = "CSV 檔案 (*.csv)|*.csv|純文字檔 (*.txt)|*.txt|所有檔案 (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    drinksMenu.Clear();

                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            string drinkName = parts[0].Trim();
                            if (int.TryParse(parts[1].Trim(), out int price))
                            {
                                drinksMenu[drinkName] = price; // 索引器指派：若重複則覆蓋，確保 Key 唯一
                            }
                        }
                    }

                    GenerateDrinkMenuUI();
                    MessageBox.Show($"成功載入 {drinksMenu.Count} 項飲料菜單！", "訊息", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"讀取檔案失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 4. 動態生成 UI (StackPanel, CheckBox, Label, Slider) 與 5. 動態 Data Binding (SetBinding)
        private void GenerateDrinkMenuUI()
        {
            DrinkMenuStackPanel.Children.Clear();

            foreach (KeyValuePair<string, int> item in drinksMenu)
            {
                StackPanel rowPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5),
                    Height = 40,
                    Background = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };

                CheckBox chkDrink = new CheckBox
                {
                    Content = item.Key,
                    Width = 180,
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                Label lbPrice = new Label
                {
                    Content = $"${item.Value} 元",
                    Width = 90,
                    FontSize = 14,
                    Foreground = Brushes.DimGray,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Slider sliderQty = new Slider
                {
                    Width = 140,
                    Minimum = 1,
                    Maximum = 20,
                    Value = 1,
                    TickFrequency = 1,
                    IsSnapToTickEnabled = true,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                Label lbAmount = new Label
                {
                    Width = 80,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DarkGreen,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // 動態 Data Binding：建立 Label 與 Slider 的連動
                Binding qtyBinding = new Binding("Value")
                {
                    Source = sliderQty,
                    Mode = BindingMode.OneWay,
                    StringFormat = "{0:F0} 杯"
                };
                lbAmount.SetBinding(Label.ContentProperty, qtyBinding);

                rowPanel.Children.Add(chkDrink);
                rowPanel.Children.Add(lbPrice);
                rowPanel.Children.Add(sliderQty);
                rowPanel.Children.Add(lbAmount);

                DrinkMenuStackPanel.Children.Add(rowPanel);
            }
        }

        // 6. 視窗樹狀走訪 (DrinkMenuStackPanel.Children) 與 TryGetValue 安全讀取
        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            currentOrders.Clear();
            double totalAmount = 0.0;
            int totalCups = 0;

            foreach (UIElement child in DrinkMenuStackPanel.Children)
            {
                if (child is StackPanel rowPanel)
                {
                    CheckBox chk = rowPanel.Children.OfType<CheckBox>().FirstOrDefault();
                    Slider slider = rowPanel.Children.OfType<Slider>().FirstOrDefault();

                    if (chk != null && chk.IsChecked == true && slider != null)
                    {
                        string drinkName = chk.Content.ToString();
                        int quantity = Convert.ToInt32(slider.Value);

                        // 使用 TryGetValue 安全讀取
                        if (drinksMenu.TryGetValue(drinkName, out int unitPrice))
                        {
                            currentOrders[drinkName] = quantity;
                            totalAmount += unitPrice * quantity;
                            totalCups += quantity;
                        }
                    }
                }
            }

            if (currentOrders.Count == 0)
            {
                tbResult.Text = "【提示】尚未勾選任何品項！請點選品項 CheckBox 並調整數量。";
                lastOrderSummary = "";
                return;
            }

            // 折扣計算
            string discountMessage = "無折扣";
            double finalAmount = totalAmount;

            if (totalAmount >= 500)
            {
                discountMessage = "滿 $500 享 8 折優惠";
                finalAmount = totalAmount * 0.8;
            }
            else if (totalAmount >= 300)
            {
                discountMessage = "滿 $300 享 85 折優惠";
                finalAmount = totalAmount * 0.85;
            }
            else if (totalAmount >= 200)
            {
                discountMessage = "滿 $200 享 9 折優惠";
                finalAmount = totalAmount * 0.9;
            }

            List<string> details = new List<string>();
            int index = 1;
            foreach (var order in currentOrders)
            {
                if (drinksMenu.TryGetValue(order.Key, out int price))
                {
                    int subTotal = price * order.Value;
                    details.Add($"{index}. {order.Key}：${price} × {order.Value}杯 = ${subTotal}元");
                    index++;
                }
            }

            lastOrderSummary = $"【訂單明細 - 用餐方式：{dineType}】\n" +
                               string.Join("\n", details) +
                               $"\n----------------------------------------\n" +
                               $"總計杯數：{totalCups} 杯\n" +
                               $"原始金額：${totalAmount} 元\n" +
                               $"折扣優惠：{discountMessage}\n" +
                               $"實付總額：${finalAmount:F0} 元";

            tbResult.Text = lastOrderSummary;
        }

        // 3. SaveFileDialog 與 File.WriteAllText 匯出檔案
        private void btnExportOrder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lastOrderSummary))
            {
                MessageBox.Show("目前尚無訂單明細，請先勾選品項並點擊【計算總金額】！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "匯出訂單明細",
                Filter = "文字檔案 (*.txt)|*.txt|CSV 檔案 (*.csv)|*.csv|所有檔案 (*.*)|*.*",
                DefaultExt = ".txt",
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, lastOrderSummary);
                    MessageBox.Show("訂單明細已成功匯出！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"匯出檔案失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                dineType = rb.Content.ToString();
            }
        }
    }
}