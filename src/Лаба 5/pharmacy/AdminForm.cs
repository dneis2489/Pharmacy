﻿using pharmacy.data;
using pharmacy.service;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace pharmacy
{
    public partial class AdminController : Form
    {

        static public DataTable dtShop = new DataTable();
        private static bool changeDate = false;
        private static bool changeFactory = false;
        private static bool changeForm = false;
        private static bool changePrescription = false;

        private ShopService ShopService { get; }
        private BasketService BasketService { get; }
        private StatusService StatusService { get; }
        private StatisticsService StatisticsService { get; }
        private User User;
        private AuthorizationController authController { get;}

        private DataTable dummyData;


        public AdminController(User user, AuthorizationController authControl)
        {
            InitializeComponent();
            ShopService = ShopService.Instance;
            BasketService = BasketService.Instance;
            StatusService = StatusService.Instance;
            StatisticsService = StatisticsService.Instance;
            User = user;
            authController = authControl;
            this.dummyData = new DataTable();
        }

        private void AdminForm_Load(object sender, EventArgs e)
        {

            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;
            comboBox4.SelectedIndexChanged += comboBox4_SelectedIndexChanged;
            textBox6.TextChanged += textBox6_TextChanged;


            //Подгрузка срока годности для фильтра в разделе "Лекарства в аптеке"
            ShopService.GetMedicinesExpirationDate().ForEach(item => comboBox2.Items.Add(item));

            //Подгрузка производителей для фильтра в разделе "Лекарства в аптеке"
            ShopService.GetMedicineWithFactory().ForEach(item => comboBox3.Items.Add(item));

            //Подгрузка формы выпуска для фильтра в разделе "Лекарства в аптеке"
            ShopService.GetAllReleaseForm().ForEach(item => comboBox4.Items.Add(item));
            
            //Магазин
            ShopService.GetMedicinesInAdmin(User.PharmacyId);
            dtShop = ShopService.dtShop;
            dataGridView1.DataSource = dtShop;

            //Заказы

            refreshOrderList();

            //Статистика
            Series series = new Series("DataPoints");
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            DataTable dummyData = StatisticsService.AdminGetCountBuyMedicinesStat(User.PharmacyId);

            chart1.Series.Clear();

            foreach (DataRow row in dummyData.Rows)
            {
                DateTime date = (DateTime)row["Дата"];
                int quantity = (int)row["Значение"];
                DateTime monthYearDate = new DateTime(date.Year, date.Month, 1);

                series.Points.AddXY(monthYearDate.ToString("MM.yyyy"), quantity);
            }

            chart1.Series.Add(series);

            StatusService.GetAllName().ForEach(item => comboBox1.Items.Add(item)); //перенесено из метода TextBox_Admin_Orders_Click
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        //Раздел ЛЕКАРСТВА В АПТЕКЕ
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) //Обновление списка лекарств после выбора фильтра по сроку годности
        {
            if (comboBox2.SelectedIndex != -1)
            {
                string selectedValue = comboBox2.SelectedItem.ToString();
                if (dtShop != null)
                {
                    DataView dv = new DataView(dtShop);
                    dv.RowFilter = $"[{dataGridView1.Columns[6].HeaderText}] LIKE '%{selectedValue}%'";
                    dataGridView1.DataSource = dv;
                }

            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) //Обновление списка лекарств после выбора фильтра по производителю
        {
            if (comboBox3.SelectedIndex != -1)
            {
                string selectedValue = comboBox3.SelectedItem.ToString();
                if (dtShop != null)
                {
                    DataView dv = new DataView(dtShop);
                    dv.RowFilter = $"[{dataGridView1.Columns[12].HeaderText}] LIKE '%{selectedValue}%'";
                    dataGridView1.DataSource = dv;

                }
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e) //Обновление списка лекарств после выбора фильтра по форме выпуска
        {
            if (comboBox4.SelectedIndex != -1)
            {
                string selectedValue = comboBox4.SelectedItem.ToString();
                if (dtShop != null)
                {
                    DataView dv = new DataView(dtShop);
                    dv.RowFilter = $"[{dataGridView1.Columns[11].HeaderText}] LIKE '%{selectedValue}%'";
                    dataGridView1.DataSource = dv;

                }
            }
        }

        private void textBox6_TextChanged(object sender, EventArgs e) //Обновление списка лекарств после ввода текста с клавиатуры
        {
            if (textBox6.Text != "")
            {
                string selectedValue = textBox6.Text;
                if (dtShop != null)
                {
                    DataView dv = new DataView(dtShop);
                    dv.RowFilter = $"[{dataGridView1.Columns[2].HeaderText}] LIKE '%{selectedValue}%'";
                    dataGridView1.DataSource = dv;

                }
            }
            else
            {
                ShopService.GetMedicinesInAdmin(User.PharmacyId);
                dtShop = ShopService.dtShop;
                dataGridView1.DataSource = dtShop;
            }
        }

        private void button5_Click(object sender, EventArgs e) //Товары в магазине - сбросить фильтры
        {
            comboBox2.SelectedIndex = -1;
            comboBox3.SelectedIndex = -1;
            comboBox4.SelectedIndex = -1;
            textBox6.Text = "";

            changeDate = false;
            changeFactory = false;
            changeForm = false;
            changePrescription = false;

            ShopService.GetMedicinesInAdmin(User.PharmacyId);
            dtShop = ShopService.dtShop;
            dataGridView1.DataSource = dtShop;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        //Раздел ЗАКАЗЫ
        private void TextBox_Admin_Orders_Click(object sender, EventArgs e) //Выбор заказа в разделе Заказы и подгрузка содержимого заказа
        {
            // Обработка события Click элемента TextBox
            System.Windows.Forms.TextBox clickedTextBox = (System.Windows.Forms.TextBox)sender;

            int startIndex = clickedTextBox.Text.IndexOf("Номер заказа: ") + "Номер заказа: ".Length;
            int endIndex = clickedTextBox.Text.IndexOf(Environment.NewLine, startIndex);
            int id;

            if (startIndex != -1 && endIndex != -1)
            {
                id = Int32.Parse(clickedTextBox.Text.Substring(startIndex, endIndex - startIndex));
                BasketService.GetBasketMedicines(id);
            }

            startIndex = clickedTextBox.Text.IndexOf("Имя заказчика: ") + "Имя заказчика: ".Length;
            endIndex = clickedTextBox.Text.IndexOf(Environment.NewLine, startIndex);
            textBox1.Text = clickedTextBox.Text.Substring(startIndex, endIndex - startIndex);

            

            startIndex = clickedTextBox.Text.IndexOf("Номер заказа: ") + "Номер заказа: ".Length;
            endIndex = clickedTextBox.Text.IndexOf(Environment.NewLine, startIndex);
            textBox3.Text = clickedTextBox.Text.Substring(startIndex, endIndex - startIndex);

            startIndex = clickedTextBox.Text.IndexOf("Дата доставки: ") + "Дата доставки: ".Length;
            endIndex = clickedTextBox.Text.IndexOf(Environment.NewLine, startIndex);
            textBox4.Text = clickedTextBox.Text.Substring(startIndex, endIndex - startIndex);

            startIndex = clickedTextBox.Text.IndexOf("Статус заказа: ") + "Статус заказа: ".Length;
            endIndex = clickedTextBox.Text.IndexOf(Environment.NewLine, startIndex);
            textBox5.Text = clickedTextBox.Text.Substring(startIndex, endIndex - startIndex);

            dataGridView2.DataSource = BasketService.dtBasket;
            dataGridView2.Columns["id"].Visible = false;

            textBox2.Text = dataGridView2.Rows
            .Cast<DataGridViewRow>()
                .Sum(row => Convert.ToDecimal(row.Cells["Стоимость:"].Value)).ToString();
        }

        private void button1_Click(object sender, EventArgs e) //Кнопка "Сохранить новый статус" в разделе "Заказы"
        {
            if (textBox3.Text != "")
            {
                if (comboBox1.SelectedIndex != -1)
                {
                    BasketService.UpdateStatusByNameAndBasketNumber(textBox3.Text, comboBox1.SelectedItem.ToString());
                    textBox5.Text = comboBox1.SelectedItem.ToString();
                    refreshOrderList();
                }
                else
                {
                    MessageBox.Show("Не выбран статус заказа!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
            }
            else
            {
                MessageBox.Show("Не выбран заказ!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
        }

        //Обновление перечня заказов
        private void refreshOrderList()
        {
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel1.AutoScroll = true;
            System.Windows.Forms.TextBox textBox;
            List<string> ordersInfos = BasketService.GetOrdersInfosByPharmacyId(User.PharmacyId);
            foreach (var item in ordersInfos)
            {
                textBox = new System.Windows.Forms.TextBox()
                {
                    Multiline = true,
                    Size = new System.Drawing.Size(126, 120), // Устанавливаем размеры текстового поля
                    ReadOnly = true
                };
                textBox.MouseEnter += (sender, e) => { ((Control)sender).Cursor = Cursors.Hand; };
                textBox.Text = item;

                textBox.Click += TextBox_Admin_Orders_Click; // Обработчик события нажатия на текстовое поле

                flowLayoutPanel1.Controls.Add(textBox); // Добавляем TextBox в FlowLayoutPanel
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        //Раздел СТАТИСТИКА
        private void button2_Click(object sender, EventArgs e) //Статистика - Количество купленного товара в магазине
        {
            dummyData.Clear();
            StatisticsService.dtStat.Clear();
            StatisticsService.dtStat2.Clear();

            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            ChartArea chartArea = new ChartArea();
            chart1.ChartAreas.Add(chartArea);

            chart1.Visible = true;
            dataGridView3.Visible = false;


            Series series = new Series("DataPoints");
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            dummyData = StatisticsService.AdminGetCountBuyMedicinesStat(User.PharmacyId);

            chart1.Series.Clear();

            foreach (DataRow row in dummyData.Rows)
            {
                DateTime date = (DateTime)row["Дата"];
                int quantity = (int)row["Значение"];
                DateTime monthYearDate = new DateTime(date.Year, date.Month, 1);

                series.Points.AddXY(monthYearDate.ToString("MM.yyyy"), quantity);
            }

            chart1.Series.Add(series);
        }

        private void button3_Click(object sender, EventArgs e) //Статистика - Количество покупок в магазине
        {
            dummyData.Clear();
            StatisticsService.dtStat.Clear();
            StatisticsService.dtStat2.Clear();

            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            ChartArea chartArea = new ChartArea();
            chart1.ChartAreas.Add(chartArea);

            chart1.Visible = true;
            dataGridView3.Visible = false;

            Series series = new Series("DataPoints");
            series.ChartType = SeriesChartType.Line;
            series.MarkerStyle = MarkerStyle.Circle;
            dummyData = StatisticsService.AdminGetCountBasketStat(User.PharmacyId);

            chart1.Series.Clear();

            foreach (DataRow row in dummyData.Rows)
            {
                DateTime date = (DateTime)row["Дата"];
                int quantity = (int)row["Значение"];
                DateTime monthYearDate = new DateTime(date.Year, date.Month, 1);

                series.Points.AddXY(monthYearDate.ToString("MM.yyyy"), quantity);
            }

            chart1.Series.Add(series);
        }

        private void button4_Click(object sender, EventArgs e) //Рейтинг покупателей
        {
            dummyData.Clear();
            StatisticsService.dtStat.Clear();
            StatisticsService.dtStat2.Clear();

            chart1.Visible = false;
            dataGridView3.Visible = true;
            StatisticsService.getTopUsersInPharmacy(User.PharmacyId);
            dataGridView3.DataSource = StatisticsService.dtStat;

        }

        //Экспорт лекарств
        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            saveFileDialog.Title = "Сохранить файл Excel";
            saveFileDialog.FileName = "Отчет по товарам.xlsx"; // Имя файла по умолчанию

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ExcelExport.ExportDataFromDataTable((DataTable)dataGridView1.DataSource, saveFileDialog, ShopService.dataColumns);
                }
                catch
                {
                    MessageBox.Show("Файл не сохранен", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                Console.WriteLine("Сохранение файла отменено.");
            }
            Console.WriteLine("Сохранение файла отменено.");
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        //ВЫХОД
        private void button6_Click(object sender, EventArgs e) //Кнопка ВЫХОД
        {
            authController.Show();
            this.Close();
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        //Закрытие приложения
        private void CloseButton_Click(object sender, FormClosingEventArgs e)
        {
            // Завершаем процесс приложения
            Application.Exit();
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        //ПУСТЫЕ МЕТОДЫ БЕЗ РЕАЛИЗАЦИИ
        private void textBox6_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void pharmacy_Click(object sender, EventArgs e)
        {

        }

        private void AdminController_FormClosing(object sender, FormClosingEventArgs e)
        {
            authController.Show();
        }

        //Экспорт статистики
        private void button8_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            saveFileDialog.Title = "Сохранить файл Excel";
            saveFileDialog.FileName = "Отчет по статистике.xlsx"; // Имя файла по умолчанию

            DataTable exportData = new DataTable();

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (dummyData.Rows.Count != 0)
                    {
                        exportData = dummyData;
                    }
                    else if (StatisticsService.dtStat.Rows.Count != 0)
                    {
                        exportData = StatisticsService.dtStat;
                    }
                    else
                    {
                        exportData = StatisticsService.dtStat2;
                    }

                    ExcelExport.ExportDataFromStat(exportData, saveFileDialog);
                }
                catch
                {
                    MessageBox.Show("Ошибка", "Файл не сохранен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                Console.WriteLine("Сохранение файла отменено.");
            }
            Console.WriteLine("Сохранение файла отменено.");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}
