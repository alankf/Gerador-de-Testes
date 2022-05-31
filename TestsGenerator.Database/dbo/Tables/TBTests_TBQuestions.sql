CREATE TABLE [dbo].[TBTests_TBQuestions] (
    [Test_Id]     INT NOT NULL,
    [Question_Id] INT NOT NULL,
    CONSTRAINT [FK_Questions_Tests] FOREIGN KEY ([Question_Id]) REFERENCES [dbo].[TBQuestions] ([Id]),
    CONSTRAINT [FK_Tests_Questions] FOREIGN KEY ([Test_Id]) REFERENCES [dbo].[TBTests] ([Id])
);

