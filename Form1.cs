using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shon
{
    public struct Master //Хранение связки "мастер - оказываемые им услуги"
    {
        string Name;
        string[] Service;
        string[] Post;
        public int serviceLength { get { return Service.Length; } }

        public string this[int index] //индексатор для обращения к улугам
        {
            get { return Service[index]; }
        }

        public void Set(string name, string[] post, string[] service)
        {
            Name = name;
            Post = post;
            Service = service;
        }

        public (string, string[], string[]) Get()
        {
            return (Name, Post, Service);
        }

        public string GetName()
        {
            return Name;
        }

        public string[] GetService()
        {
            return Service;
        }
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        SqlConnection connection; //экземпляр класса связи бд с прогрой
        SqlCommand command; //экземпляр для передачи комманд
        SqlDataReader reader = null; //экземпляр для чтения бд
        string link; //строка соединения
        Master[] worker; //массив структур, хранящих в себе мастеров и инфу о них
        string date;
        bool base_loaded = false;
        bool access = false;
        Form2 form2;
        public async void DataBase() // чтение базы для поиска доступного времени
        {
            reader = null;

            command = new SqlCommand("SELECT * FROM [Journal]", connection);

            string[] possible_time = new string[] { "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00" };

            if (dateTimePicker1.Value.CompareTo(DateTime.Now) <= 0) 
            { 
                int now = DateTime.Now.Hour;
                for (int i = 0; i < possible_time.Length; i++)
                {
                    if (Convert.ToInt32(possible_time[i].Remove(2)) <= now) possible_time[i] = null;
                    else if (Convert.ToInt32(possible_time[i].Remove(2)) <= now + 1) { possible_time[i] = null; break; }
                }
            }
            
            comboBox3.Items.Clear();
            try
            {
                reader = await command.ExecuteReaderAsync();
                int i = 0;
                string[] booking_time = new string[possible_time.Length];
                while(await reader.ReadAsync())
                {
                    if(reader["Date"].ToString().Remove(10) == date && reader["Master"].ToString() == comboBox2.Text)
                    {
                        booking_time[i] = reader["Time"].ToString().Remove(5);
                        for (int j = 0; j < possible_time.Length; j++)
                        {
                            if (possible_time[j] == booking_time[i] && booking_time[i] != null)
                                possible_time[j] = null;
                        }
                        i++;
                    }    
                }

                i = 0;
                while (i < possible_time.Length) 
                {
                    if (possible_time[i] != null) comboBox3.Items.Add(possible_time[i]);
                    i++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString(), e.Source.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            if (comboBox3.Items.Count == 0) { selectATimeLabel.Text = "На сегодня записаться не получится"; customerInformation.Visible = false; }
            else
            {
                selectATimeLabel.ForeColor = Color.White;
                selectATimeLabel.Text = "Выберите время приёма:";
                comboBox3.Visible = true;
            }
        }

        public async void DataBase(string search) // чтение базы для поиска совпадающих значений
        {
            reader = null;

            command = new SqlCommand("SELECT * FROM [Journal]", connection);

            journalTable.Rows.Clear();

            try
            {
                reader = await command.ExecuteReaderAsync();
                if (search == null)
                {
                    if (!switchFilterByDate.Checked) 
                        while (await reader.ReadAsync())
                        {
                            journalTable.Rows.Add(reader["Surname"], reader["Name"], reader["Phone"], reader["Date"].ToString().Remove(11) + reader["Time"].ToString().Remove(5), 
                                reader["Service"], reader["Master"], reader["id"]);
                        }
                    else 
                        while (await reader.ReadAsync()) 
                        { 
                            if (reader["Date"].ToString().Remove(10) == filterByDate.Value.ToShortDateString())
                                journalTable.Rows.Add(reader["Surname"], reader["Name"], reader["Phone"], reader["Date"].ToString().Remove(11) + reader["Time"].ToString().Remove(5),
                                    reader["Service"], reader["Master"], reader["id"]);
                        }
                }
                else
                {
                    search = Punctuation(search.ToLower());
                    if(!switchFilterByDate.Checked) 
                        while (await reader.ReadAsync())
                        {
                            string maybe = Punctuation(reader["Surname"].ToString().ToLower() + reader["Name"].ToString().ToLower() + reader["Phone"].ToString() + reader["Date"].ToString().Remove(11) + 
                                reader["Time"].ToString().Remove(5).ToLower() + reader["Service"].ToString().ToLower() + reader["Master"].ToString().ToLower());
                            if (maybe.Contains(search)) journalTable.Rows.Add(reader["Surname"], reader["Name"], reader["Phone"], reader["Date"].ToString().Remove(11) + reader["Time"].ToString().Remove(5),
                                reader["Service"], reader["Master"], reader["id"]);
                        }
                    else 
                        while(await reader.ReadAsync())
                        {
                            if(reader["Date"].ToString().Remove(10) == filterByDate.Value.ToShortDateString()) 
                            {
                                string maybe = Punctuation(reader["Surname"].ToString().ToLower() + reader["Name"].ToString().ToLower() + reader["Phone"].ToString() + reader["Date"].ToString().Remove(11) +
                                   reader["Time"].ToString().Remove(5).ToLower() + reader["Service"].ToString().ToLower() + reader["Master"].ToString().ToLower());
                                if (maybe.Contains(search)) journalTable.Rows.Add(reader["Surname"], reader["Name"], reader["Phone"], reader["Date"].ToString().Remove(11) + reader["Time"].ToString().Remove(5),
                                    reader["Service"], reader["Master"], reader["id"]);
                            }
                        }
                }    
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString(), e.Source.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            SortJournal();
            Coloring();
        }

        public async void AddToDataBase(string surname, string name, string phone) // добавление в базу данных (запись)
        {
            string service = choosingAService.Text;
            string master = comboBox2.Text;
            string time = comboBox3.Text;
            

            command = new SqlCommand("INSERT INTO [Journal] (Master, Service, Date, Time, Surname, Name, Phone)VALUES(@Master, @Service, @Date, @Time, @Surname, @Name, @Phone)", connection);

            command.Parameters.AddWithValue("Master", master);
            command.Parameters.AddWithValue("Service", service);
            command.Parameters.AddWithValue("Date", dateTimePicker1.Value);
            command.Parameters.AddWithValue("Time", time + ":00");
            command.Parameters.AddWithValue("Surname", surname);
            command.Parameters.AddWithValue("Name", name);
            command.Parameters.AddWithValue("Phone", phone);

            await command.ExecuteNonQueryAsync();

            dateTimePicker1.Value = DateTime.Now;

            MessageBox.Show($"Наименнование услуги: {service}\nМастер: {master}\nЗапись на имя: {name} {surname}\nДата: {date}\tВремя: {time}", "Запись оформлена", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        
            choosingAService.SelectedIndex = -1;
            customerInformation.Visible = false;
            selectAMasterLabel.Visible = false;
            comboBox2.Visible = false;
            selectADateLabel.Visible = false;
            comboBox3.Visible = false;
            dateTimePicker1.Visible = false;
            selectATimeLabel.Visible = false;
            surnameField.Text = null;
            nameField.Text = null;
            phoneField.Text = null;
            base_loaded = false;
            comboBox3.Visible = false;
        }

        async void DataBaseUpdate (int id, string change, string change_adress) //изменение данных клиентов
        {
            switch (change_adress)
            {
                case "Surname":
                    command = new SqlCommand("UPDATE [Journal] SET [Surname]=@Surname WHERE [id]=@id", connection);
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("Surname", change);
                    await command.ExecuteNonQueryAsync();
                    break;

                case "Name":
                    command = new SqlCommand("UPDATE [Journal] SET [Name]=@Name WHERE [id]=@id", connection);
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("Name", change);
                    await command.ExecuteNonQueryAsync();
                    break;

                case "Phone":
                    command = new SqlCommand("UPDATE [Journal] SET [Phone]=@Phone WHERE [id]=@id", connection);
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithValue("Phone", change);
                    await command.ExecuteNonQueryAsync();
                    break;
            }
        }

        async void DeleteFromDataBase(int id)
        {
            command = new SqlCommand("DELETE FROM [Journal] WHERE [id]=@id", connection);

            command.Parameters.AddWithValue("id", id);

            await command.ExecuteNonQueryAsync();
        }

        void SortJournal()
        {
            DateTime[] q = new DateTime[journalTable.Rows.Count];
            DateTime best_time = new DateTime(9999, 01, 01);

            int i = 0;
            int[] placements = new int[q.Length];
            bool[] used = new bool[q.Length];

            while (i < q.Length)
            {
                int year = Convert.ToInt32(journalTable["Date", i].Value.ToString().Remove(0, 6).Remove(4));
                int month = Convert.ToInt32(journalTable["Date", i].Value.ToString().Remove(0, 3).Remove(2));
                int day = Convert.ToInt32(journalTable["Date", i].Value.ToString().Remove(2));
                int hour = Convert.ToInt32(journalTable["Date", i].Value.ToString().Remove(0, 11).Remove(2));
                q[i] = new DateTime(year, month, day, hour, 0, 0);
                placements[i] = -1;
                i++;
            }

            i = 0;
            string[,] s = new string[journalTable.Columns.Count, journalTable.Rows.Count];
            while (i < q.Length)
            {           
                for(int k = 0; k < q.Length; k++)
                    if (!used[k])   
                        if (q[k].CompareTo(best_time) < 0) 
                        { 
                            placements[i] = k; 
                            best_time = q[k]; 
                        }

                for (int j = 0; j < journalTable.Columns.Count; j++)  s[j, i] = journalTable[j, placements[i]].Value.ToString(); 

                best_time = new DateTime(9999, 01, 01);
                used[placements[i]] = true;
                i++;
            }

            journalTable.Rows.Clear();

            i = 0;
            while (i < q.Length)
            {
                journalTable.Rows.Add(s[0, i], s[1, i], s[2, i], s[3, i], s[4, i], s[5, i], s[6, i]);
                i++;
            }      
        }

        string default_link = @"C:\Приложение для записи к парикмахерскую\Database1.mdf";

        private void Form1_Load(object sender, EventArgs e)
        {
            
            link = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + default_link + @";Integrated Security=True";

            journalTab.Visible = false;
            mainPanel.Visible = true;
            servicesPanel.Visible = false;
            staffPanel.Visible = false;
            journalPanel.Visible = false;
            panel5.Visible = false;

            mainTab.BackColor = Color.MediumOrchid;
            servicesTab.BackColor = Color.Transparent;
            mastersTab.BackColor = Color.Transparent;
            journalTab.BackColor = Color.Transparent;
            recordTab.BackColor = Color.Transparent;

            if(!System.IO.File.Exists(default_link))     //проверка на наличие файла по стандартному пути          
            {
                openFileDialog1.Filter = "SQL DataBase (*mdf)|*.mdf";
                if (openFileDialog1.ShowDialog() == DialogResult.Cancel) { Error("База данных не подключена. Пожалуйста, презапустите приложение"); return; }
                default_link = @openFileDialog1.FileName;
                link = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + @openFileDialog1.FileName + @";Integrated Security=True";
            }

            connection = new SqlConnection(link);
            connection.OpenAsync();

            //Инициализация мастеров
            worker = new Master[11];
            worker[0].Set("Кирилл", new string[] { "Мастер ногтевого сервиса" }, new string[] { "Маникюр", "Педикюр" });
            worker[1].Set("Ростык", new string[] { "Мастер ногтевого сервиса" }, new string[] { "Маникюр", "Педикюр" });
            worker[2].Set("Паша", new string[] { "Детский парикмахер" }, new string[] { "Детская стрижка" });
            worker[3].Set("Дима", new string[] { "Парикмахер-универсал", "Барбер" }, new string[] { "Женская стрижка", "Мужская стрижка", "Стрижка бороды" });
            worker[4].Set("Ромчик", new string[] { "Парикмахер-колорист", "Парикмахер-стилист", "Барбер" }, new string[] { "Окрашивание волос", "Завивка", "Стрижка бороды" });
            worker[5].Set("Ирвин", new string[] { "Парикмахер-стилист" }, new string[] { "Коррекция бровей" });
            worker[6].Set("Нина", new string[] { "Мастер ногтевого сервиса", "Парикмахер-колорист" }, new string[] { "Маникюр", "Педикюр", "Окрашивание волос" });
            worker[7].Set("Егор", new string[] { "Парикмахер-универсал", "Детский парикмахер" }, new string[] { "Женская стрижка", "Мужская стрижка", "Детская стрижка" });
            worker[8].Set("Данил", new string[] { "Парикмахер-универсал" }, new string[] { "Женская стрижка", "Мужская стрижка" });
            worker[9].Set("Лиза", new string[] { "Парикмахер-стилист", "Женский парихмахер" }, new string[] { "Коррекция бровей", "Женская стрижка" });
            worker[10].Set("Артём", new string[] { "Мужской парикмахер", "Барбер" }, new string[] { "Мужская стрижка", "Стрижка бороды" });

            staffList.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            for(int i = 0; i < staffList.Columns.Count; i++)
            {
                staffList.Columns[i].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                staffList.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            foreach (Master a in worker)
            {
                (string name, string[] _post, string[] _service) = a.Get();
                string post = null;
                string service = null;
                foreach (string post_ in _post) post += post_ + ", \n";
                foreach (string service_ in _service) service += service_ + ", \n";
                post = post.TrimEnd(' ', ',', '\n');
                service = service.TrimEnd(' ', ',', '\n');
                staffList.Rows.Add(name, post, service);
            }
            staffList.Sort(staffList.Columns[0], 0);

            dateTimePicker1.MinDate = dateTimePicker1.Value; //нижняя граница записи: сегодня
            dateTimePicker1.MaxDate = dateTimePicker1.Value.AddYears(1); //верхняя граница: через год

            journalTable.Columns.Add("Surname", "Фамилия");
            journalTable.Columns.Add("Name", "Имя");
            journalTable.Columns.Add("Phone", "Номер телефона");
            journalTable.Columns.Add("Date", "Дата и время");
            journalTable.Columns.Add("Service", "Услуга");
            journalTable.Columns.Add("Master", "Мастер");
            journalTable.Columns.Add("id", "id");

            journalTable.Columns["Surname"].Width = 100;
            journalTable.Columns["Name"].Width = 100;
            journalTable.Columns["Phone"].Width = 80;
            journalTable.Columns["Date"].Width = 100;
            journalTable.Columns["Service"].Width = 110;

            journalTable.Columns["Service"].ReadOnly = true;
            journalTable.Columns["id"].ReadOnly = true;
            journalTable.Columns["Master"].ReadOnly = true;
            journalTable.Columns["Date"].ReadOnly = true;

            foreach (DataGridViewColumn column in journalTable.Columns)
                column.SortMode = DataGridViewColumnSortMode.NotSortable;

            form2 = new Form2();
        }

        private void mainTab_Click(object sender, EventArgs e) // главная
        {
            splitContainer1.Panel1.BackColor=Color.FromArgb(64, 64, 64);
            mainPanel.Visible = true;
            servicesPanel.Visible = false;
            staffPanel.Visible = false;
            journalPanel.Visible = false;
            panel5.Visible = false;
            mainTab.BackColor = Color.MediumOrchid;
            servicesTab.BackColor = Color.Transparent;
            mastersTab.BackColor = Color.Transparent;
            journalTab.BackColor = Color.Transparent;
            recordTab.BackColor = Color.Transparent;
        }

        private void servicesTab_Click(object sender, EventArgs e) // услуги
        {
            splitContainer1.Panel1.BackColor = Color.BlueViolet;
            mainPanel.Visible = false;
            servicesPanel.Visible = true;
            staffPanel.Visible = false;
            journalPanel.Visible = false;
            panel5.Visible = false;
            mainTab.BackColor = Color.Transparent;
            servicesTab.BackColor = Color.MediumOrchid;
            mastersTab.BackColor = Color.Transparent;
            journalTab.BackColor = Color.Transparent;
            recordTab.BackColor = Color.Transparent;
        }

        private void mastersTab_Click(object sender, EventArgs e) // мастера
        {
            if (System.IO.File.Exists(default_link))
            {
                splitContainer1.Panel1.BackColor = Color.MediumSeaGreen;
                mainPanel.Visible = false;
                servicesPanel.Visible = false;
                staffPanel.Visible = true;
                journalPanel.Visible = false;
                panel5.Visible = false;
                mainTab.BackColor = Color.Transparent;
                servicesTab.BackColor = Color.Transparent;
                mastersTab.BackColor = Color.MediumOrchid;
                journalTab.BackColor = Color.Transparent;
                recordTab.BackColor = Color.Transparent;
            }
            else Error("Отсуствие подключения к базе данных");
        }
        private void journalTab_Click(object sender, EventArgs e) //журнал
        {
            if(System.IO.File.Exists(default_link))
            {
                splitContainer1.Panel1.BackColor = Color.SlateGray;
                mainPanel.Visible = false;
                servicesPanel.Visible = false;
                staffPanel.Visible = false;
                journalPanel.Visible = true;
                panel5.Visible = false;
                mainTab.BackColor = Color.Transparent;
                servicesTab.BackColor = Color.Transparent;
                mastersTab.BackColor = Color.Transparent;
                journalTab.BackColor = Color.MediumOrchid;
                recordTab.BackColor = Color.Transparent;
                updateButton_Click(null, null);
            }
            else Error("Отсуствие подключения к базе данных");
        }
        private void recordTab_Click(object sender, EventArgs e) //запись
        {
            if (System.IO.File.Exists(default_link))
            {
                splitContainer1.Panel1.BackColor = Color.Purple;
                mainPanel.Visible = false;
                servicesPanel.Visible = false;
                staffPanel.Visible = false;
                journalPanel.Visible = false;
                panel5.Visible = true;
                mainTab.BackColor = Color.Transparent;
                servicesTab.BackColor = Color.Transparent;
                mastersTab.BackColor = Color.Transparent;
                journalTab.BackColor = Color.Transparent;
                recordTab.BackColor = Color.MediumOrchid;
            }
            else Error("Отсуствие подключения к базе данных");
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) // выбор оказываемой услуги
        {
            customerInformation.Visible = false;
            selectADateLabel.Visible = false;
            comboBox3.Visible = false;
            dateTimePicker1.Visible = false;
            selectATimeLabel.Visible = false;

            comboBox2.Items.Clear();

            for (int i = 0; i < worker.Length; i++)
            {
                for (int j = 0; j < worker[i].serviceLength; j++)
                {
                    if (worker[i][j] == choosingAService.Text) comboBox2.Items.Add(worker[i].GetName());
                }

            }

            selectAMasterLabel.Visible = true;
            comboBox2.Visible = true;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)  // Выбор мастера
        {
            dateTimePicker1.Visible = true;
            selectADateLabel.Visible = true;
            if (base_loaded) dateTimePicker1_ValueChanged(sender, e);
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) //выбор даты записи
        {
            comboBox3.Visible = false;
            date = dateTimePicker1.Value.ToShortDateString();
            selectATimeLabel.Visible = true;
            selectATimeLabel.ForeColor = Color.Yellow;
            selectATimeLabel.Text = "Производится поиск, пожалуйста подождите";
            DataBase();
            base_loaded = true;
        }

        private void recordButton_Click(object sender, EventArgs e) //записаться
        {
            StringBuilder str;

            if (string.IsNullOrEmpty(surnameField.Text)) { Error("Введите фамилию"); return; }
            if (string.IsNullOrEmpty(nameField.Text)) { Error("Введите имя"); return; }
            if (string.IsNullOrEmpty(phoneField.Text)) { Error("Введите номер телефона"); return; }

            str = new StringBuilder(surnameField.Text);
            string surname = Punctuation(str,"Фамилия");
            if (surname == null) return;

            str = new StringBuilder(nameField.Text);
            string name = Punctuation(str, "Имя");
            if (name == null) return;
            
            str = new StringBuilder("+7" + phoneField.Text);
            string phone = Punctuation(str);
            if (phone == null) return;

            AddToDataBase(surname, name, phone);

            base_loaded = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
                connection.Close();
        }

        void Error(string error)
        {
            MessageBox.Show(error, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        string Punctuation(StringBuilder str)
        {
            string marks = "(),.!?- ";

            for(int i = 0; i < str.Length; i++)
            {
                for (int j = 0; j < marks.Length; j++)
                    if (str[i] == marks[j])
                    {
                        str.Remove(i, 1);
                        i--;
                    }
            }

            for (int k = 1; k < str.Length; k++) { if (!char.IsDigit(str[k])) { Error("Номер должен состоять только из цифр"); return null; } }
            if (str.Length != 12) { Error("Номер должен состоять из 11 цифр"); return null; }
            return str.ToString();
        }

        string Punctuation(StringBuilder str, string topic)
        {
            string marks = "(),.!?- ";

            for (int i = 0; i < str.Length; i++)
            {
                for (int j = 0; j < marks.Length; j++)
                    if (str[i] == marks[j])
                    {
                        str.Remove(i, 1);
                        i--;
                    }
                if (char.IsDigit(str[i])) { Error(topic + " не может содержать цифры"); return null; }
            }
            return str.ToString();
        }

        string Punctuation(string st)
        {
            StringBuilder str = new StringBuilder(st);
            string marks = "(),.!?- ";

            for (int i = 0; i < str.Length; i++)
            {
                for (int j = 0; j < marks.Length; j++)
                    if (str[i] == marks[j])
                    {
                        str.Remove(i, 1);
                        i--;
                    }
            }
            return str.ToString();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            customerInformation.Visible = true;
            //customerInformation.Location = new Point(customerInformation.Location.X, customerInformation.Location.Y + 100);
        }

        private void searchLine_KeyDown(object sender, KeyEventArgs e) // поиск по журналу
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(searchLine.Text) || !string.IsNullOrWhiteSpace(searchLine.Text))
                {
                    DataBase(searchLine.Text);
                }
            }
            if (e.KeyCode == Keys.F5)
            {
                if (string.IsNullOrEmpty(searchLine.Text) || string.IsNullOrWhiteSpace(searchLine.Text)) DataBase(null);
                else DataBase(searchLine.Text);
            }
        }

        private void switchFilterByDate_CheckedChanged(object sender, EventArgs e) // включение/выключение фильстра по дате
        {
            if (switchFilterByDate.Checked) filterByDate.Enabled = true;
            else filterByDate.Enabled = false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) // горячая главиша "обновить"
        {
            if (e.KeyCode == Keys.F5) 
            { 
                if(string.IsNullOrEmpty(searchLine.Text) || string.IsNullOrWhiteSpace(searchLine.Text)) DataBase(null); 
                else DataBase(searchLine.Text);
            }
        }

        private void deletFromSearchLine_Click(object sender, EventArgs e) // очистить поле поиска
        {
            searchLine.Text = null;
        }

        private void journalTable_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e) //удаление записи из базы данных
        {
            if (MessageBox.Show("Вы точно уверены, что хотите удалить из базы данных текущую запись?", "Внимание!!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DeleteFromDataBase(Convert.ToInt32(journalTable["id", e.Row.Index].Value));

                MessageBox.Show("Удаление прошло успешно", "Внимание!!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else e.Cancel = true;
        }

        string cell_content;

        private void journalTable_CellEndEdit(object sender, DataGridViewCellEventArgs e) // проверка на редактирование
        {
            if (journalTable[e.ColumnIndex, e.RowIndex].Value.ToString() != cell_content)
            {
                if (MessageBox.Show("Вы точно уверены, что хотите изменить содержимое этой ячейки?", "Внимание!!!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DataBaseUpdate(Convert.ToInt32(journalTable["id", e.RowIndex].Value), journalTable[e.ColumnIndex, e.RowIndex].Value.ToString(), journalTable.Columns[e.ColumnIndex].Name);
                    MessageBox.Show("Данные были успешно обновлены!", "Внимание!!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else journalTable[e.ColumnIndex, e.RowIndex].Value = cell_content;
            }
        }

        private void journalTable_CellEnter(object sender, DataGridViewCellEventArgs e) // считывание содержимого ячейки
        {
            cell_content = journalTable[e.ColumnIndex, e.RowIndex].Value.ToString();
        }

        private void updateButton_Click(object sender, EventArgs e) // обновить
        {
            if (string.IsNullOrEmpty(searchLine.Text) || string.IsNullOrWhiteSpace(searchLine.Text)) DataBase(null);
            else DataBase(searchLine.Text);
        }

        void Coloring()
        {
            int year, month, day, hour;
            for(int i = 0; i < journalTable.Rows.Count; i++)
            {
                year = Convert.ToInt32(journalTable[journalTable.Columns["Date"].Index,i].Value.ToString().Remove(0,6).Remove(4));
                month = Convert.ToInt32(journalTable[journalTable.Columns["Date"].Index, i].Value.ToString().Remove(0, 3).Remove(2));
                day = Convert.ToInt32(journalTable[journalTable.Columns["Date"].Index, i].Value.ToString().Remove(2));
                hour = Convert.ToInt32(journalTable[journalTable.Columns["Date"].Index, i].Value.ToString().Remove(0, 11).Remove(2));

                DateTime cell = new DateTime(year, month, day, hour, 0, 0);
                if (cell.CompareTo(DateTime.Now) < 0) for (int j = 0; j < journalTable.Columns.Count; j++) journalTable[j, i].Style.BackColor = Color.Red;
                if (cell.CompareTo(DateTime.Now) > 0) for (int j = 0; j < journalTable.Columns.Count; j++) journalTable[j, i].Style.BackColor = Color.Green;
                if (cell.Day == DateTime.Now.Day && cell.Month == DateTime.Now.Month && cell.Year == DateTime.Now.Year && cell.Hour > DateTime.Now.Hour) 
                    for (int j = 0; j < journalTable.Columns.Count; j++) journalTable[j, i].Style.BackColor = Color.LightGreen;
            }
        }

        private void staffList_CellDoubleClick(object sender, DataGridViewCellEventArgs e) // поиск клиентов мастера
        {
            if (access)
            {
                if (e.ColumnIndex == 0 && e.RowIndex != -1)
                {
                    searchLine.Text = staffList[e.ColumnIndex, e.RowIndex].Value.ToString();
                    journalTab_Click(sender, e);
                }
            }
        }

        private void updateButton_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                if (string.IsNullOrEmpty(searchLine.Text) || string.IsNullOrWhiteSpace(searchLine.Text)) DataBase(null);
                else DataBase(searchLine.Text);
            }
        }

        private void logo_DoubleClick(object sender, EventArgs e)
        {
            if (!access)
            {
                form2.ShowDialog();
                if (form2.DialogResult == DialogResult.Cancel)
                {
                    access = form2.Access;
                    if (!access) Error("Вы не авторизованы");
                    else journalTab.Visible = true;
                }
            }
        }
    }
}
