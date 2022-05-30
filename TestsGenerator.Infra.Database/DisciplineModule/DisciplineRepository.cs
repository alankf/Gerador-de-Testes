using FluentValidation;
using FluentValidation.Results;
using System.Data.SqlClient;
using TestsGenerator.Domain.DisciplineModule;
using TestsGenerator.Infra.Database.MateriaModule;
using TestsGenerator.Infra.Database.Shared;

namespace TestsGenerator.Infra.Database.DisciplineModule
{
    public class DisciplineRepository : IRepository<Discipline>
    {
        private const string connectionString =
            @"Data Source=(LocalDB)\MSSqlLocalDB;
              Initial Catalog=DatabaseTest;
              Integrated Security=True;
              Pooling=False";

        private SqlConnection? conn = null;

        private readonly MateriaRepository _materiaRepository;

        public DisciplineRepository(MateriaRepository materiaRepository)
        {
            _materiaRepository = materiaRepository;
        }

        public ValidationResult Insert(Discipline discipline)
        {
            ValidationResult validationResult = GetValidator().Validate(discipline);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query = @"INSERT INTO [TBDISCIPLINES] ([NAME]) VALUES (@NAME)";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", discipline.Id);
                command.Parameters.AddWithValue("NAME", discipline.Name);

                discipline.Id = Convert.ToInt32(command.ExecuteScalar());

                return validationResult;
            }
        }

        public ValidationResult Update(Discipline discipline)
        {
            ValidationResult validationResult = GetValidator().Validate(discipline);

            if (validationResult.IsValid == false)
                return validationResult;

            using (conn = new(connectionString))
            {
                string query = @"UPDATE [TBDISCIPLINES] SET [NAME] = @NAME WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", discipline.Id);
                command.Parameters.AddWithValue("NAME", discipline.Name);

                command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public ValidationResult Delete(Discipline discipline)
        {
            using (conn = new(connectionString))
            {
                string query = @"DELETE FROM [TBDISCIPLINES] WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                conn.Open();

                command.Parameters.AddWithValue("ID", discipline.Id);

                ValidationResult validationResult = new();

                if (_materiaRepository.GetAll().Select(x => x.Discipline).Contains(discipline))
                    validationResult.Errors.Add(new ValidationFailure("", "Não é possível remover esta disciplina, pois ela está relacionada a uma matéria."));

                if (validationResult.IsValid)
                    command.ExecuteNonQuery();

                return validationResult;
            }
        }

        public List<Discipline> GetAll()
        {
            using (conn = new(connectionString))
            {
                string query = @"SELECT * FROM [TBDISCIPLINES]";

                using SqlCommand command = new(query, conn);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                List<Discipline> disciplines = new();

                while (reader.Read())
                {
                    Discipline discipline = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = Convert.ToString(reader["NAME"])
                    };

                    disciplines.Add(discipline);
                }

                return disciplines;
            }
        }

        public Discipline? GetById(int id)
        {
            using (conn = new(connectionString))
            {
                string query = @"SELECT * FROM [TBDISCIPLINES] WHERE [ID] = @ID";

                using SqlCommand command = new(query, conn);

                command.Parameters.AddWithValue("ID", id);

                conn.Open();

                using SqlDataReader reader = command.ExecuteReader();

                Discipline? discipline = null;

                if (reader.Read())
                    discipline = new()
                    {
                        Id = Convert.ToInt32(reader["ID"]),
                        Name = Convert.ToString(reader["Name"])
                    };

                return discipline;
            }
        }

        public AbstractValidator<Discipline> GetValidator()
        {
            return new DisciplineValidator();
        }
    }
}