using FluentValidation;
using FluentValidation.Results;
using TestsGenerator.Domain.DisciplineModule;
using TestsGenerator.Infra.Shared;

namespace TestsGenerator.Infra.DisciplineModule
{
    public class DisciplineRepository : BaseRepository<Discipline>
    {
        public DisciplineRepository(DataContext dataContext) : base(dataContext) { }

        public override List<Discipline> GetRegisters()
        {
            return _dataContext.Disciplines;
        }

        public override AbstractValidator<Discipline> GetValidator()
        {
            return new DisciplineValidator();
        }

        public override ValidationResult Update(Discipline t)
        {
            AbstractValidator<Discipline> validator = GetValidator();

            ValidationResult validationResult = validator.Validate(t);

            if (validationResult.IsValid == false)
                return validationResult;

            List<Discipline> registers = GetRegisters();

            bool existsName = registers.Select(x => x.Name).Contains(t.Name, StringComparer.OrdinalIgnoreCase);

            if (existsName && t.Id == 0)
                validationResult.Errors.Add(new ValidationFailure("", "Nome já está cadastrado"));

            if (validationResult.IsValid)
            {
                registers.ForEach(x =>
                {
                    if (x.Id == t.Id)
                        x.Update(t);
                });
            }

            return validationResult;
        }
    }
}