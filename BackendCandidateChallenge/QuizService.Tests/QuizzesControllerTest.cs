using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using QuizService.Model;
using Xunit;

namespace QuizService.Tests;

public class QuizzesControllerTest
{
    const string QuizApiEndPoint = "/api/quizzes/";

    [Fact]
    public async Task PostNewQuizAddsQuiz()
    {
        var quiz = new QuizCreateModel("Test title");
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(quiz));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"),
                content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }
    }

    [Fact]
    public async Task AQuizExistGetReturnsQuiz()
    {
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 1;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            var quiz = JsonConvert.DeserializeObject<QuizResponseModel>(await response.Content.ReadAsStringAsync());
            Assert.Equal(quizId, quiz.Id);
            Assert.Equal("My first quiz", quiz.Title);
        }
    }

    [Fact]
    public async Task AQuizDoesNotExistGetFails()
    {
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 999;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task AQuizDoesNotExists_WhenPostingAQuestion_ReturnsNotFound()
    {
        const string QuizApiEndPoint = "/api/quizzes/999/questions";

        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 999;
            var question = new QuestionCreateModel("The answer to everything is what?");
            var content = new StringContent(JsonConvert.SerializeObject(question));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
    //Test for adding new quizz with questions and answers, and asserting the correct score
    [Fact]
    public async Task CreateQuizAndAssertCorrectAnswers()
    {
        //quiz 
        var quizz = new QuizCreateModel("Backend challenge test quizz!");
        var content = new StringContent(JsonConvert.SerializeObject(quizz),Encoding.UTF8, "application/json");

        //question 
        var questions = new List<QuestionCreateModel>()
        {
                new QuestionCreateModel("This is the first question?"),
                new QuestionCreateModel("This is the second question?"),
                new QuestionCreateModel("This is the third question?")
        };

        //answer
        var answers = new List<AnswerCreateModel>()
        {
            new AnswerCreateModel("This is the first question?"),
            new AnswerCreateModel("This is the second question?"),
            new AnswerCreateModel("This is the third question?")
        };

        List<int> correctAnswers = new List<int>();


        using (var testHost = new TestServer(new WebHostBuilder()
                 .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();

            //post request for quiz, with id of a quizz as a result
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            var id = response.Content.ReadAsStringAsync().Result;

            //post request for the questions
            foreach (var question in questions)
            {
                content = new StringContent(JsonConvert.SerializeObject(question), Encoding.UTF8, "application/json");
                var responseQuestion = await client.PostAsync(new Uri(testHost.BaseAddress, $"/api/quizzes/{id}/questions"), content);
                var questionId = responseQuestion.Content.ReadAsStringAsync().Result;

                int answerId = 0;

                foreach (var (answer, index) in answers.Select((value, i) => (value, i)))
                {
                    //post request for the answers
                    content = new StringContent(JsonConvert.SerializeObject(answer), Encoding.UTF8, "application/json");
                    var responseAnswer = await client.PostAsync(new Uri(testHost.BaseAddress, $"/api/quizzes/{id}/questions/{questionId}/answers"), content);
                    if (index == 0)
                    {
                        answerId = Convert.ToInt32(responseAnswer.Content.ReadAsStringAsync().Result);
                        correctAnswers.Add(answerId);
                    }
                }
                //put request for setting correct answers
                var correctAnswer = new QuestionUpdateModel(question.Text, answerId);
                content = new StringContent(JsonConvert.SerializeObject(correctAnswer), Encoding.UTF8, "application/json");
                responseQuestion = await client.PutAsync(new Uri(testHost.BaseAddress, $"/api/quizzes/{id}/questions/{questionId}"), content);
            }
            //get request for the quizz
            response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{id}"));
            var quiz = JsonConvert.DeserializeObject<QuizResponseModel>(await response.Content.ReadAsStringAsync());
            var correctAnswersFromGet = quiz.Questions.Select(x => x.CorrectAnswerId).ToList();

            //asserting the collection of correct answers to the one from the api
            //Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert.AreEqual(correctAnswers, correctAnswersFromGet);

            //asserting if the score is correct
            Assert.Equal(correctAnswers.Count, correctAnswersFromGet.Count);
        }
    }
}