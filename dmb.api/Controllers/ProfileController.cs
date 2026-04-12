using Microsoft.AspNetCore.Mvc;

namespace dmb.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                name = "Deo Bernal",
                summary = "Senior Full-Stack .NET Developer with 19+ years of experience building scalable web applications using .NET, React, Angular, and SQL.\r\nExperienced in ERP, banking, and enterprise systems with strong focus on performance and\r\nmaintainability",
                video = "https://go.screenpal.com/watch/cOflXKnOnrx",
                skills = new[]
                {
                    "C#", ".NET Core", "AngularJS", "Angular", "React", ".NET Core C#", "SQL", "cloud-native technologies",
                    "AI-assisted development", "Cursor", "GitHub Copilot", "React Native", "ASP.NET C#", "MVC", "MS SQL", "MongoDB", "Docker",
                    "RabbitMQ", "Google Cloud Platform", "HubSpot", "PingIdentity", "REST APIs",
                    "Azure", "Cloud Functions", "Datastore", "Pub/Sub", "ARM/Bicep-style deployments", "WCF", "EF6", "IoC/DI", "Bootstrap", "Umbraco", "ASP Classic",
                    "JavaScript", "Flash", "Telerik controls", "ORM", "XML", "SQL Server", "AJAX", "VB6", "Crystal Reports", "VB.NET"
                },
                projectCategories = new[]
                {
                    new
                    {
                        title = "🧳 Travel Industry Systems",
                        items = new[]
                        {
                            new { name = "Enterprise Management System", description = "Travel agent management platform" },
                            new { name = "Content Management System (Travel)", description = "CMS for travel operations" }
                        }
                    },
                    new
                    {
                        title = "💰 Finance & Banking Systems",
                        items = new[]
                        {
                            new { name = "MortgageChoice", description = "Brokerage management system" },
                            new { name = "CustomerData.Contact", description = "Cloud-based customer data microservices (CRM sync)" },
                            new { name = "Tardis Omniverse", description = "Lead generation platform" },
                            new { name = "Rubik", description = "Banking and credit card management system" },
                            new { name = "Internet Banking System", description = "Online banking platform (web & mobile)" }
                        }
                    },
                    new
                    {
                        title = "🌐 Web & CMS Platforms",
                        items = new[]
                        {
                            new { name = "Content Management System (Umbraco)", description = "CMS for websites" },
                            new { name = "Project Management System (AngularJS)", description = "Internal team management tool" }
                        }
                    },
                    new
                    {
                        title = "🏢 Enterprise & Business Systems",
                        items = new[]
                        {
                            new { name = "Enterprise Accounting & HR System", description = "ERP for accounting, HR, purchasing" },
                            new { name = "Training Management System", description = "Employee training platform" },
                            new { name = "Document Management System", description = "File storage, search, and PDF export system" },
                            new { name = "Client/Project/Task Management System", description = "Business workflow system" }
                        }
                    },
                    new
                    {
                        title = "🛒 E-Commerce & Customer-Facing Apps",
                        items = new[]
                        {
                            new { name = "eCommerce Platform (AspDotNetStorefront)", description = "Online shopping system" },
                            new { name = "Payment Gateway Integration Enhancements", description = "Fixes and improvements" }
                        }
                    }
                },
                contact = new
                {
                    email = "deobernal@gmail.com",
                    phone = "+63 925 455 6063"
                }
            });
        }
    }
}
