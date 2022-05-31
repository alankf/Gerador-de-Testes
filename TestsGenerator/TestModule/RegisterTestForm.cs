using FluentValidation.Results;
using System.Data;
using TestsGenerator.Domain.DisciplineModule;
using TestsGenerator.Domain.MateriaModule;
using TestsGenerator.Domain.QuestionModule;
using TestsGenerator.Domain.Shared;
using TestsGenerator.Domain.TestModule;

namespace TestsGenerator.TestModule
{
    public partial class RegisterTestForm : Form
    {
        private Test test;
        private readonly List<Question> _questions;
        private readonly List<Materia> _materias;

        public RegisterTestForm(List<Discipline> disciplines, List<Materia> materias, List<Question> questions)
        {
            InitializeComponent();

            _materias = materias;
            _questions = questions;

            disciplines.ForEach(x => CbxDiscipline.Items.Add(x));
        }

        public Test Test
        {
            get { return test; }

            set 
            { 
                test = value;

                TxbTitle.Text = test.Title;
                CbxDiscipline.SelectedItem = test.Discipline;
                CbxGrade.SelectedItem = test.Grade;
                CbxBimester.SelectedItem = test.Bimester;
                CbxMateria.SelectedItem = test.Materia;

                int i = 0;

                for (int j = 0; j < ClbxAvailableQuestions.Items.Count; i++)
                {
                    Question question = (Question)ClbxAvailableQuestions.Items[j];

                    if (test.Questions.Contains(question))
                        ClbxAvailableQuestions.SetItemChecked(i, true);

                    i++;
                }
            }
        }

        public List<Question> SelectedQuestions
        {
            get
            {
                return ClbxAvailableQuestions.CheckedItems.Cast<Question>().ToList();
            }
        }

        public List<Question> NonSelectedQuestions
        {
            get
            {
                return ClbxAvailableQuestions.Items.Cast<Question>().Except(SelectedQuestions).ToList();
            }
        }

        public Func<Test, List<Question>, ValidationResult> InsertRecord { get; set; }
        public Func<Test, List<Question>, List<Question>, ValidationResult> EditRecord { get; set; }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            test.Title = TxbTitle.Text;
            test.Discipline = (Discipline)CbxDiscipline.SelectedItem;
            test.Grade = (string)CbxGrade.SelectedItem;

            if (CbxBimester.SelectedItem != null)
                test.Bimester = (Bimester)CbxBimester.SelectedItem;

            test.Materia = (Materia)CbxMateria.SelectedItem;

            ValidationResult? validationResult = null;

            if (test.Id == 0)
                validationResult = InsertRecord(test, SelectedQuestions);
            else
                validationResult = EditRecord(test, SelectedQuestions, NonSelectedQuestions);

            if (validationResult.IsValid == false)
            {
                MessageBox.Show(validationResult.ToString("\n"), Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                DialogResult = DialogResult.None;
            }
        }

        private void CbxDiscipline_SelectedIndexChanged(object sender, EventArgs e)
        {
            CbxGrade.Items.Clear();
            CbxBimester.Items.Clear();
            CbxMateria.Items.Clear();
            ClbxAvailableQuestions.Items.Clear();

            List<string> grades = _materias
                .Where(x => x.Discipline.Equals(CbxDiscipline.SelectedItem))
                .Select(y => y.Grade)
                .Distinct()
                .ToList();

            grades.ForEach(x => CbxGrade.Items.Add(x));
        }

        private void CbxGrade_SelectedIndexChanged(object sender, EventArgs e)
        {
            CbxBimester.Items.Clear();
            CbxMateria.Items.Clear();

            List<Bimester> bimesters = Enum
                .GetValues(typeof(Bimester))
                .Cast<Bimester>()
                .ToList();

            bimesters.ForEach(x => CbxBimester.Items.Add(x));
        }

        private void CbxBimester_SelectedIndexChanged(object sender, EventArgs e)
        {
            CbxMateria.Items.Clear();

            List<Materia> materias = _materias
                .Where(x =>
                x.Discipline.Equals(CbxDiscipline.SelectedItem) &&
                x.Grade == (string)CbxGrade.SelectedItem &&
                x.Bimester == (Bimester)CbxBimester.SelectedItem)
                .ToList();

            materias.ForEach(x => CbxMateria.Items.Add(x));
        }

        private void CbxMateria_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<Question> questions = _questions
                .Where(x =>
                x.Discipline.Equals(CbxDiscipline.SelectedItem) &&
                x.Grade == (string)CbxGrade.SelectedItem &&
                x.Bimester == (Bimester)CbxBimester.SelectedItem &&
                x.Materia.Equals(CbxMateria.SelectedItem))
                .ToList();

            questions.ForEach(x => ClbxAvailableQuestions.Items.Add(x));
        }
    }
}