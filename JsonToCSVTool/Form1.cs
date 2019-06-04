using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace JsonToCSVTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "全部文件(*.txt;*.json)|*.txt;*.json";
            var ret = ofd.ShowDialog();
            if (ret == DialogResult.Cancel)
                return;
            var file = ofd.FileName;
            var content = File.ReadAllText(file, Encoding.Default);
            load(content);
        }

        private void BtnPaste_Click(object sender, EventArgs e)
        {
            var content = Clipboard.GetText();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("剪贴板中没有文字内容。");
                return;
            }
            load(content);
        }

        private JArray findArray(JObject obj)
        {
            foreach (var token in obj.PropertyValues())
            {
                if (token is JArray)
                    return (JArray)token;
                else if (token is JObject)
                {
                    var tmpArray = findArray((JObject)token);
                    if (tmpArray != null)
                        return tmpArray;
                }
            }
            return null;
        }

        private void load(string json)
        {
            JArray items = null;
            try
            {
                var token = JToken.Parse(json);
                if (token is JArray)
                    items = (JArray)token;
                else if (token is JObject)
                {
                    items = findArray((JObject)token);
                }
                if (items == null)
                    throw new ApplicationException("items is null");
            }
            catch
            {
                MessageBox.Show("导入文本不是一个有效的JSON格式，或者JSON对象中没有包括数组！");
                return;
            }
            try
            {                
                DataTable dt = new DataTable();
                bool columnAdded = false;
                foreach (JObject item in items)
                {
                    //添加列
                    if (!columnAdded)
                    {
                        columnAdded = true;
                        foreach (var property in item.Properties())
                        {
                            dt.Columns.Add(property.Name);
                        }
                    }
                    List<object> valueList = new List<object>();
                    foreach (JValue value in item.PropertyValues())
                        valueList.Add(value.Value);

                    dt.Rows.Add(valueList.ToArray());
                }
                dgvMain.DataSource = dt;
                btnSaveToCSVFile.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void BtnSaveToCSVFile_Click(object sender, EventArgs e)
        {
            var dt = dgvMain.DataSource as DataTable;
            if (dt == null)
            {
                MessageBox.Show("当前没有数据！");
                return;
            }
            var sfd = new SaveFileDialog();
            sfd.Filter = "CSV文件(*.csv)|*.csv";
            var ret = sfd.ShowDialog();
            if (ret == DialogResult.Cancel)
                return;
            using (var stream = File.OpenWrite(sfd.FileName))
            using (var writer = new StreamWriter(stream, Encoding.Default))
            {
                List<string> list = new List<string>();
                foreach (DataColumn column in dt.Columns)
                {
                    list.Add(column.ColumnName);
                }
                writer.WriteLine(string.Join(",", list.ToArray()));
                foreach (DataRow row in dt.Rows)
                {
                    list.Clear();
                    foreach (var item in row.ItemArray)
                        list.Add(item.ToString());
                    writer.WriteLine(string.Join(",", list.ToArray()));
                }
                writer.Close();
            }
            MessageBox.Show("保存成功！");
        }
    }
}
