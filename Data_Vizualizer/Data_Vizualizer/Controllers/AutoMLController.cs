using System;
using System.Web.Mvc;
using Microsoft.ML;
using Data_VizualizerML.Model;
using Data_Vizualizer.Models;

namespace Data_Vizualizer.Controllers
{
    public class AutoMLController : Controller
    {
        [HttpGet]
        public ActionResult Analysis()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Analysis(ModelInput input,FormCollection fc)
        {
            // Load the model  
            MLContext mlContext = new MLContext();
            ITransformer mlModel = mlContext.Model.Load(Server.MapPath("~/Content/MLModel.zip"), out var modelInputSchema);
            // Create predection engine related to the loaded train model
            var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
            //Input  
            input.Review_body = fc["message"].ToString();
            // Try model on sample data and find the score
            ModelOutput result = predEngine.Predict(input);
            // Store result into ViewBag
            ViewBag.Result = result;
            double score = result.Score;
            string type = "";
            if (score >= 3.3)
            {
                type = "Good";
            }
            else if (score < 3.3)
            {
                type = "Bad";
            }
            ApplicationDbContext db = new ApplicationDbContext();
            Feedbacks ft = new Feedbacks();

            ft.Names = User.Identity.Name;
            ft.Date = DateTime.Now;
            ft.FeedBack = input.Review_body;
            ft.Typeof = type;
            db.Feedbacks.Add(ft);
            int count = 0;
            if (count == 0)
            {
                db.SaveChanges();
                count = 1;
            }
            return Redirect("/HomePage/Index");
        }
    }
}