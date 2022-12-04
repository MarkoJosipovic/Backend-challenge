namespace QuizService.Model;

public class QuestionUpdateModel
{
    //added constructor to the model
    public QuestionUpdateModel(string text, int correctAnswerId)
    {
        Text = text;
        CorrectAnswerId = correctAnswerId;
    }

    public string Text { get; set; }
    public int CorrectAnswerId { get; set; }
}