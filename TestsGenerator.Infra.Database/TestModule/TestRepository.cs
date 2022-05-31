using FluentValidation;
using FluentValidation.Results;
using System.Data.SqlClient;
using TestsGenerator.Domain.DisciplineModule;
using TestsGenerator.Domain.QuestionModule;
using TestsGenerator.Domain.Shared;
using TestsGenerator.Domain.TestModule;
using TestsGenerator.Infra.Database.Shared;

namespace TestsGenerator.Infra.Database.TestModule
{
    public class TestRepository : ITestRepository
    {
        private const string connectionString =
            @"Data Source=(LocalDB)\MSSqlLocalDB;
              Initial Catalog=DatabaseTest;
              Integrated Security=True;
              Pooling=False";

        private SqlConnection? conn = null;

        public ValidationResult Insert(Test test)
        {
            ValidationResult validationResult = GetValidator().Validate(test);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query = 
                    @"INSERT INTO [TBTESTS]
                        (
                            [TITLE],
                            [GRADE],
                            [BIMESTER],
                            [MATERIA_ID]
                        )

                        VALUES

                        (
                            @TITLE,
                            @GRADE,
                            @BIMESTER,
                            @MATERIA_ID
                        )
                        
                        SELECT SCOPE_IDENTITY()";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("TITLE", test.Title);
                command.Parameters.AddWithValue("GRADE", test.Grade);
                command.Parameters.AddWithValue("BIMESTER", test.Bimester);
                command.Parameters.AddWithValue("MATERIA_ID", test.Materia.Id);

                test.Id = Convert.ToInt32(command.ExecuteScalar());

                return validationResult;
            }
        }

        public ValidationResult Insert(Test test, List<Question> questions)
        {
            ValidationResult validationResult = Insert(test);

            if (validationResult.IsValid == false)
                return validationResult;

            questions.ForEach(x => AddQuestion(test, x));

            return validationResult;
        }

        public ValidationResult Update(Test test)
        {
            ValidationResult validationResult = GetValidator().Validate(test);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query =
                    @"UPDATE [TBTESTS]
                        SET
                            [TITLE] = @TITLE,
                            [GRADE] = @GRADE,
                            [BIMESTER] = @BIMESTER,
                            [MATERIA_ID] = @MATERIA_ID
                        WHERE
                            [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", test.Id);
                command.Parameters.AddWithValue("TITLE", test.Title);
                command.Parameters.AddWithValue("GRADE", test.Grade);
                command.Parameters.AddWithValue("BIMESTER", test.Bimester);
                command.Parameters.AddWithValue("MATERIA_ID", test.Materia.Id);

                command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public ValidationResult Update(Test test, List<Question> selectedQuestions, List<Question> nonSelectedQuestions)
        {
            ValidationResult validationResult = Update(test);

            if (validationResult.IsValid == false)
                return validationResult;

            nonSelectedQuestions.ForEach(x => RemoveQuestion(x));

            selectedQuestions.ForEach(x => AddQuestion(test, x));

            return validationResult;
        }

        public ValidationResult Delete(Test test)
        {
            using (conn = new(connectionString))
            {
                string query = 
                    @"DELETE FROM [TBTESTS] WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", test.Id);

                test.Questions.ForEach(x => RemoveQuestion(x));
                test.Questions.Clear();

                int registersToDelete = command.ExecuteNonQuery();

                ValidationResult validationResult = new();

                if (registersToDelete == 0)
                    validationResult.Errors.Add(new ValidationFailure("", "Não foi possível remover o registro"));

                return validationResult;
            }
        }

        public List<Test> GetAll()
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"SELECT
	                        T.ID,
	                        T.TITLE,
	                        T.GRADE,
	                        T.BIMESTER,
	                        T.MATERIA_ID,

	                        MT.NAME MATERIA_NAME,
	                        MT.GRADE MATERIA_GRADE,
	                        MT.BIMESTER MATERIA_BIMESTER,

	                        D.ID DISCIPLINE_ID,
	                        D.NAME DISCIPLINE_NAME

                        FROM [TBTests] T 

	                        INNER JOIN 
	                        [TBMATERIAS] MT 

                        ON 
	                        T.MATERIA_ID = MT.ID 

	                        INNER JOIN 
	                        [TBDISCIPLINES] D 

                        ON 
	                        MT.DISCIPLINE_ID = D.ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                List<Test> tests = new();

                while (reader.Read())
                {
                    Discipline discipline = new()
                    {
                        Id = Convert.ToInt32(reader["DISCIPLINE_ID"]),
                        Name = Convert.ToString(reader["DISCIPLINE_NAME"])
                    };

                    Test test = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Title = Convert.ToString(reader["TITLE"]),
                        Grade = Convert.ToString(reader["GRADE"]),
                        Bimester = (Bimester)reader["BIMESTER"],
                        Discipline = discipline,
                        Materia = new()
                        {
                            Id = Convert.ToInt32(reader["MATERIA_ID"]),
                            Name = Convert.ToString(reader["MATERIA_NAME"]),
                            Grade = Convert.ToString(reader["MATERIA_GRADE"]),
                            Bimester = (Bimester)reader["MATERIA_BIMESTER"],
                            Discipline = discipline
                        }
                    };

                    tests.Add(test);

                    LoadQuestions(test);
                }

                return tests;
            }

        }

        private void LoadQuestions(Test test)
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"SELECT
	                        QT.ID,
	                        QT.DESCRIPTION,
	                        QT.GRADE,
	                        QT.BIMESTER,
	                        QT.MATERIA_ID,
	
	                        MT.NAME MATERIA_NAME,
	                        MT.GRADE MATERIA_GRADE,
	                        MT.BIMESTER MATERIA_BIMESTER,

	                        D.ID DISCIPLINE_ID,
	                        D.NAME DISCIPLINE_NAME

                        FROM [TBQUESTIONS] QT 

	                        INNER JOIN
	                        [TBMATERIAS] MT

                        ON QT.MATERIA_ID = MT.ID

	                        INNER JOIN
	                        [TBDISCIPLINES] D

                        ON MT.DISCIPLINE_ID = D.ID

	                        INNER JOIN 
	                        [TBTESTS_TBQUESTIONS] TQ 

                        ON 
	                        QT.ID = TQ.QUESTION_ID

                        WHERE
	                        TQ.TEST_ID = @TEST_ID";

                using SqlCommand command = new(query, conn);

                command.Parameters.AddWithValue("TEST_ID", test.Id);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Discipline discipline = new()
                    {
                        Id = Convert.ToInt32(reader["DISCIPLINE_ID"]),
                        Name = Convert.ToString(reader["DISCIPLINE_NAME"])
                    };

                    Question question = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Description = Convert.ToString(reader["DESCRIPTION"]),
                        Grade = Convert.ToString(reader["GRADE"]),
                        Bimester = (Bimester)reader["BIMESTER"],
                        Discipline = discipline,
                        Materia = new()
                        {
                            Id = Convert.ToInt32(reader["MATERIA_ID"]),
                            Name = Convert.ToString(reader["MATERIA_NAME"]),
                            Grade = Convert.ToString(reader["MATERIA_GRADE"]),
                            Bimester = (Bimester)reader["MATERIA_BIMESTER"],
                            Discipline = discipline
                        }
                    };

                    test.AddQuestion(question);
                }
            }
        }

        private void AddQuestion(Test test, Question question)
        {
            using (conn = new(connectionString))
            {
                string query = 
                    @"INSERT INTO [TBTESTS_TBQUESTIONS]
                        (
                            [TEST_ID],
                            [QUESTION_ID]
                        )
                        
                        VALUES
                        
                        (
                            @TEST_ID,
                            @QUESTION_ID
                        )";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("TEST_ID", test.Id);
                command.Parameters.AddWithValue("QUESTION_ID", question.Id);

                command.ExecuteNonQuery();
            }
        }

        private void RemoveQuestion(Question question)
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"DELETE FROM [TBTESTS_TBQUESTIONS] WHERE [QUESTION_ID] = @QUESTION_ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("QUESTION_ID", question.Id);

                command.ExecuteNonQuery();
            }
        }

        public AbstractValidator<Test> GetValidator()
        {
            return new TestValidator();
        }

    }
}