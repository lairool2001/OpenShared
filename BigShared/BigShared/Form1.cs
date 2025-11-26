namespace BigShared
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string[] linez;
        int index = 0;
        float wordIndex;
        string line;
        private void Form1_Load(object sender, EventArgs e)
        {
            linez = File.ReadAllLines("¸}¥».txt");
            index = 0;
            next();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            next();
        }
        void next()
        {
            if (index >= linez.Length)
            {
                index = linez.Length - 1;
                line = linez[index];
                return;
            }
            line = linez[index];
            index++;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (wordIndex >= line.Length)
            {
                richTextBox1.Text = line;
                timer1.Stop();
                wordIndex = 0;
                return;
            }
            richTextBox1.Text = line.Substring(0, (int)wordIndex);
            wordIndex += 1000f / timer1.Interval;
        }
    }
}
