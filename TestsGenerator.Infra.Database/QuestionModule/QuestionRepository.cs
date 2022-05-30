using FluentValidation;
using FluentValidation.Results;
using System.Data.SqlClient;
using TestsGenerator.Domain.AlternativeModule;
using TestsGenerator.Domain.DisciplineModule;
using TestsGenerator.Domain.QuestionModule;
using TestsGenerator.Domain.Shared;
using TestsGenerator.Infra.Database.Shared;
using TestsGenerator.Infra.TestModule;

namespace TestsGenerator.Infra.Database.QuestionModule
{
    public class QuestionRepository : IRepository<Question>
    {
        private const string connectionString =
            @"Data Source=(LocalDB)\MSSqlLocalDB;
              Initial Catalog=DatabaseTest;
              Integrated Security=True;
              Pooling=False";

        private SqlConnection? conn = null;

        private readonly TestRepository _testRepository;

        public QuestionRepository(TestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public ValidationResult Insert(Question question)
        {
            ValidationResult validationResult = GetValidator().Validate(question);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query =
                    @"INSERT INTO [TBQUESTIONS]
                        (
                            [DESCRIPTION],
                            [GRADE],
                            [BIMESTER],
                            [MATERIA_ID]
                        )
                        
                        VALUES
            
                        (
                            @DESCRIPTION,
                            @GRADE,
                            @BIMESTER,
                            @MATERIA_ID
                        )
                        
                        SELECT SCOPE_IDENTITY()";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("DESCRIPTION", question.Description);
                command.Parameters.AddWithValue("GRADE", question.Grade);
                command.Parameters.AddWithValue("BIMESTER", question.Bimester);
                command.Parameters.AddWithValue("MATERIA_ID", question.Materia.Id);

                question.Id = Convert.ToInt32(command.ExecuteScalar());

                return validationResult;
            }
        }

        public ValidationResult Update(Question question)
        {
            ValidationResult validationResult = GetValidator().Validate(question);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query =
                    @"UPDATE [TBQUESTIONS]
                        SET
                            [DESCRIPTION] = @DESCRIPTION,
                            [GRADE] = @GRADE,
                            [BIMESTER] = @BIMESTER,
                            [MATERIA_ID] = @MATERIA_ID
                        WHERE
                            [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", question.Id);
                command.Parameters.AddWithValue("DESCRIPTION", question.Description);
                command.Parameters.AddWithValue("GRADE", question.Grade);
                command.Parameters.AddWithValue("BIMESTER", question.Bimester);
                command.Parameters.AddWithValue("MATERIA_ID", question.Materia.Id);

                command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public ValidationResult Delete(Question question)
        {
            using (conn = new(connectionString))
            {
                string query = @"DELETE FROM [TBQUESTIONS] WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", question.Id);

                ValidationResult validationResult = new();

                _testRepository.GetRegisters().Select(x => x.Questions).ToList().ForEach(x =>
                {
                    if (x.Contains(question))
                        validationResult.Errors.Add(new ValidationFailure("", "Não é possível remover esta questão, pois ela está relacionada a um teste."));
                });

                question.Alternatives.ForEach(x => DeleteAlternative(x));
                question.Alternatives.Clear();
                command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public List<Question> GetAll()
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

                        ON 
	                        QT.MATERIA_ID = MT.ID 

	                        INNER JOIN 
	                        [TBDISCIPLINES] D 

                        ON 
	                        MT.DISCIPLINE_ID = D.ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                List<Question> questions = new();

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

                    questions.Add(question);

                    LoadQuestionAlternatives(question);
                }

                return questions;
            }
        }

        public AbstractValidator<Question> GetValidator()
        {
            return new QuestionValidator();
        }

        private void LoadQuestionAlternatives(Question question)
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"SELECT 
	                        [ID],
	                        [LETTER],
	                        [ISCORRECT],
                            [DESCRIPTION],
	                        [QUESTION_ID]
                        FROM 
	                        [TBQUESTIONSALTERNATIVES]
                        WHERE
	                        [QUESTION_ID] = @QUESTION_ID";

                using SqlCommand command = new(query, conn);

                command.Parameters.AddWithValue("QUESTION_ID", question.Id);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Alternative alternative = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Letter = Convert.ToString(reader["LETTER"]),
                        IsCorrect = Convert.ToBoolean(reader["ISCORRECT"]),
                        Description = Convert.ToString(reader["DESCRIPTION"])
                    };

                    question.AddAlternative(alternative);
                }
            }
        }

        public void AddAlternatives(Question question, List<Alternative> alternatives)
        {
            using (conn = new(connectionString))
            {
                conn.Open();

                string query =
                    @"INSERT INTO TBQUESTIONSALTERNATIVES
                        (
                            [LETTER],
                            [ISCORRECT],
                            [DESCRIPTION],
                            [QUESTION_ID]
                        )
                        
                        VALUES
                
                        (
                            @LETTER,
                            @ISCORRECT,
                            @DESCRIPTION,
                            @QUESTION_ID
                        )
                        
                        SELECT SCOPE_IDENTITY()";

                foreach (Alternative alternative in alternatives)
                {
                    if (alternative.Id > 0)
                        continue;

                    question.AddAlternative(alternative);

                    using SqlCommand command = new(query, conn);

                    command.Parameters.AddWithValue("LETTER", alternative.Letter);
                    command.Parameters.AddWithValue("ISCORRECT", alternative.IsCorrect);
                    command.Parameters.AddWithValue("DESCRIPTION", alternative.Description);
                    command.Parameters.AddWithValue("QUESTION_ID", alternative.Question.Id);

                    alternative.Id = Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void UpdateAlternative(Alternative alternative)
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"UPDATE [TBQUESTIONSALTERNATIVES]
                        SET
                            [LETTER] = @LETTER,
                            [ISCORRECT] = @ISCORRECT,
                            [DESCRIPTION] = @DESCRIPTION,
                            [QUESTION_ID] = @QUESTION_ID
                        WHERE
                            [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", alternative.Id);
                command.Parameters.AddWithValue("LETTER", alternative.Letter);
                command.Parameters.AddWithValue("ISCORRECT", alternative.IsCorrect);
                command.Parameters.AddWithValue("DESCRIPTION", alternative.Description);
                command.Parameters.AddWithValue("QUESTION_ID", alternative.Question.Id);

                command.ExecuteNonQuery();
            }
        }

        public void DeleteAlternative(Alternative alternative)
        {
            using (conn = new(connectionString))
            {
                string query = @"DELETE FROM [TBQUESTIONSALTERNATIVES] WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", alternative.Id);

                command.ExecuteNonQuery();
            }
        }
    }
}