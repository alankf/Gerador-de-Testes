using FluentValidation;
using FluentValidation.Results;
using System.Data.SqlClient;
using TestsGenerator.Domain.MateriaModule;
using TestsGenerator.Domain.Shared;
using TestsGenerator.Infra.Database.QuestionModule;
using TestsGenerator.Infra.Database.Shared;

namespace TestsGenerator.Infra.Database.MateriaModule
{
    public class MateriaRepository : IRepository<Materia>
    {
        private const string connectionString =
            @"Data Source=(LocalDB)\MSSqlLocalDB;
              Initial Catalog=DatabaseTest;
              Integrated Security=True;
              Pooling=False";

        private SqlConnection? conn = null;

        private QuestionRepository _questionRepository;

        public MateriaRepository(QuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public ValidationResult Insert(Materia materia)
        {
            ValidationResult validationResult = GetValidator().Validate(materia);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query =
                    @"INSERT INTO [TBMATERIAS] 
                        (
                            [NAME],
                            [GRADE],
                            [BIMESTER],
                            [DISCIPLINE_ID]
                        ) 

                      VALUES 

                        (
                            @NAME,
                            @GRADE,
                            @BIMESTER,
                            @DISCIPLINE_ID
                        )

                       SELECT SCOPE_IDENTITY()";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", materia.Id);
                command.Parameters.AddWithValue("NAME", materia.Name);
                command.Parameters.AddWithValue("GRADE", materia.Grade);
                command.Parameters.AddWithValue("BIMESTER", materia.Bimester);
                command.Parameters.AddWithValue("DISCIPLINE_ID", materia.Discipline.Id);

                materia.Id = Convert.ToInt32(command.ExecuteScalar());

                return validationResult;
            }
        }

        public ValidationResult Update(Materia materia)
        {
            ValidationResult validationResult = GetValidator().Validate(materia);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query =
                    @"UPDATE [TBMATERIAS]
                        SET
                            [NAME] = @NAME,
                            [GRADE] = @GRADE,
                            [BIMESTER] = @BIMESTER,
                            [DISCIPLINE_ID] = @DISCIPLINE_ID
                        WHERE 
                            [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", materia.Id);
                command.Parameters.AddWithValue("NAME", materia.Name);
                command.Parameters.AddWithValue("GRADE", materia.Grade);
                command.Parameters.AddWithValue("BIMESTER", materia.Bimester);
                command.Parameters.AddWithValue("DISCIPLINE_ID", materia.Discipline.Id);

                command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public ValidationResult Delete(Materia materia)
        {
            using (conn = new(connectionString))
            {
                string query = @"DELETE FROM [TBMATERIAS] WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", materia.Id);

                ValidationResult validationResult = new();

                if (_questionRepository.GetAll().Select(x => x.Materia).Contains(materia))
                    validationResult.Errors.Add(new ValidationFailure("", "Não é possível remover esta matéria, pois ela está relacionada a uma questão."));

                if (validationResult.IsValid)
                    command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public List<Materia> GetAll()
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"SELECT
	                    MT.Id,
	                    MT.Name,
	                    MT.Grade,
	                    MT.Bimester,
	                    MT.Discipline_Id,
	                    D.Name as Discipline_Name

                      FROM 
	                    [TBMaterias] AS MT INNER JOIN
	                    [TBDisciplines] AS D ON MT.Discipline_Id = D.ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                List<Materia> materias = new();

                while (reader.Read())
                {
                    Materia materia = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = Convert.ToString(reader["NAME"]),
                        Grade = Convert.ToString(reader["GRADE"]),
                        Bimester = (Bimester)reader["BIMESTER"],
                        Discipline = new()
                        {
                            Id = Convert.ToInt32(reader["DISCIPLINE_ID"]),
                            Name = Convert.ToString(reader["DISCIPLINE_NAME"])
                        }
                    };

                    materias.Add(materia);
                }

                return materias;
            }
        }

        public Materia? GetById(int id)
        {
            using (conn = new(connectionString))
            {
                string query =
                    @"SELECT
	                    MT.ID,
	                    MT.NAME,
	                    MT.GRADE,
	                    MT.BIMESTER,
	                    MT.DISCIPLINE_ID,
	                    D.NAME as DISCIPLINE_NAME

                      FROM 
	                    [TBMaterias] AS MT INNER JOIN
	                    [TBDisciplines] AS D ON MT.DISCIPLINE_ID = D.ID

                      WHERE
                        MT.ID = @ID";

                using SqlCommand command = new(query, conn);

                command.Parameters.AddWithValue("ID", id);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                Materia? materia = null;

                if (reader.Read())
                    materia = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = Convert.ToString(reader["NAME"]),
                        Grade = Convert.ToString(reader["GRADE"]),
                        Bimester = (Bimester)reader["BIMESTER"],
                        Discipline = new()
                        {
                            Id = Convert.ToInt32(reader["DISCIPLINE_ID"]),
                            Name = Convert.ToString(reader["DISCIPLINE_NAME"])
                        }
                    };

                return materia;
            }
        }

        public AbstractValidator<Materia> GetValidator()
        {
            return new MateriaValidator();
        }
    }
}