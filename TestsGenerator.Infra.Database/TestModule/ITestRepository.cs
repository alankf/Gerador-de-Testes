using FluentValidation.Results;
using TestsGenerator.Domain.QuestionModule;
using TestsGenerator.Domain.TestModule;
using TestsGenerator.Infra.Database.Shared;

namespace TestsGenerator.Infra.Database.TestModule
{
    public interface ITestRepository : IRepository<Test> 
    {
        ValidationResult Insert(Test test, List<Question> questions);

        ValidationResult Update(Test test, List<Question> selectedQuestions, List<Question> nonSelectedQuestions);
    }
}