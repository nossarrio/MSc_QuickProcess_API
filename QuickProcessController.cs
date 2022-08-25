using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Reflection;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace QuickProcess_Core
{
    public class api : Controller
    {
        ILogger<api> _logger;

        public api(ILogger<api> logger)
        {
            _logger = logger;
        }


        /// <summary>
        /// This method handles non-ajax http GET request without any component name specified in url
        /// </summary>
        /// <returns>Returns default component of project</returns>
        [HttpGet("")]
        [Produces("text/html")]
        public async Task<IActionResult> GetApp()
        {
            string Content = "";
            try
            {
                Content = await QuickProcess.QuickProcess_Core.getApp(Request.Cookies["sessionId"]);

                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = Content
                };

            }
            catch (FileNotFoundException ex)
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = "<script>window.location='designer'</script>"
                };
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = "<script>window.location='designer'</script>"
                };
            }
        }


        /// <summary>
        /// This method handles  non-ajax http GET request when component name is specified in url
        /// </summary>
        /// <returns>Returns details of specified component</returns>
        [Route("{ComponentName}")]
        [HttpGet]
        [Produces("text/html")]
        public async Task<IActionResult> getAppComponent(String ComponentName)
        {
            return new ContentResult
            {
                ContentType = "text/html",

                Content = await QuickProcess.QuickProcess_Core.getAppComponent(ComponentName, Request.Cookies["sessionId"])
            };
        }


        /// <summary>
        /// This method handles ajax call to get details of a component
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns details of specified component</returns>
        [Route("getComponent")]
        [HttpPost]
        public async Task<string> getComponent(QuickProcess.Model.getComponent_Model request)
        {
            try
            {
                request.ComponentName = request.ComponentName.Replace("../", "");
                return JsonSerializer.Serialize(await QuickProcess.QuickProcess_Core.getComponent(request, getBaseUrl()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to fetch component: " + request.ComponentName + " " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax call to search for record for a table/card component
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns list of matching records from the datasource of the specified component</returns>
        [Route("searchRecord")]
        [HttpPost]
        public async Task<string> searchRecord(QuickProcess.Model.searchRecord_Model request)
        {
            try
            {
                var resp = await QuickProcess.QuickProcess_Core.searchRecord(request, getBaseUrl());
                return Newtonsoft.Json.JsonConvert.SerializeObject(resp);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = " Unable to search record " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax call to get record for a form component from component datasource
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns details of a matching record from the datasource of the specified component </returns>
        [Route("getRecord")]
        [HttpPost]
        public async Task<string> getRecord(QuickProcess.Model.fetchRecord_Model request)
        {
            try
            {
                var resp = await QuickProcess.QuickProcess_Core.getRecord(request, getBaseUrl());
                return JsonSerializer.Serialize(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to fetch record " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax call to save record for a form component into component datasource
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns status of operation</returns>
        [Route("saveRecord")]
        [HttpPost]
        public async Task<string> saveRecord(QuickProcess.Model.saveRecord_Model request)
        {
            try
            {
                var resp = await QuickProcess.QuickProcess_Core.saveRecord(request, getBaseUrl());
                return JsonSerializer.Serialize(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to save request " + ex.Message });
            }
        }


        /// <summary>
        /// This method hanldes ajax call to webApi, queryApi and dropdownlist component. it automatically determines component type and invokes appropriate method to handle the request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Return response from execution of the component action</returns>
        [Route("Api")]
        [HttpPost]
        public async Task<string> Api(QuickProcess.Model.api_Model request)
        {
            string componentName = "";
            try
            {
                componentName = request.ComponentName.ToString();
            }
            catch { }

            try
            {
                var componentInfo = new QuickProcess.Model.getComponent_Model()
                {
                    AppId = request.AppId,
                    ComponentName = request.ComponentName,
                    Dform = request.Dform,
                    parameters = request.parameters,
                    SessionId = request.SessionId
                };

                var component = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>((await QuickProcess.QuickProcess_Core.getComponent(componentInfo, getBaseUrl())).Result);
                
                if (component.Type.ToString() == QuickProcess.ComponentType.Query)
                {
                    var newRequest = new QuickProcess.Model.api_Model()
                    {
                        AppId = request.AppId,
                        ComponentName = request.ComponentName,
                        Dform = request.Dform,
                        parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(componentInfo.parameters).parameters.ToString(),
                        SessionId = request.SessionId
                    };                                     
                    var resp = await QuickProcess.QuickProcess_Core.QueryApi(newRequest, getBaseUrl());
                    return JsonSerializer.Serialize(resp);
                }

                if (component.Type.ToString() == QuickProcess.ComponentType.API)
                {
                    var newRequest = new QuickProcess.Model.api_Model()
                    {
                        AppId = request.AppId,
                        ComponentName = request.ComponentName,
                        Dform = request.Dform,
                        parameters = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(componentInfo.parameters).parameters.ToString(),
                        SessionId = request.SessionId
                    };
                    var resp = await QuickProcess.QuickProcess_Core.WebApi(newRequest, getBaseUrl());
                    return JsonSerializer.Serialize(resp);
                }

                if (component.Type.ToString() == QuickProcess.ComponentType.DropDownList)
                {
                    var newRequest = new QuickProcess.Model.getDropDownList_Model()
                    {
                        AppId = request.AppId,
                        ComponentName = request.ComponentName,
                        Dform = request.Dform,
                        parameters = request.parameters,
                        SearchText = request.SearchText,
                        SearchValue = request.SearchValue,
                        SessionId = request.SessionId
                    };
                    var resp = await QuickProcess.QuickProcess_Core.getDropDownList(newRequest, getBaseUrl());
                    return JsonSerializer.Serialize(resp);
                }

                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "404", ResponseDescription = "Component '" + componentName + "' was not found " });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to execute api '" + componentName + "' " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax call to execute QueryApI component script
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns response from the execution of QueryAPI script</returns>
        [Route("QueryApi")]
        [HttpPost]
        public async Task<string> QueryApi(QuickProcess.Model.api_Model request)
        {
            string componentName = "";
            try
            {
                componentName = request.ComponentName.ToString();
            }
            catch { }

            try
            {
                var resp = await QuickProcess.QuickProcess_Core.QueryApi(request, getBaseUrl());
                return JsonSerializer.Serialize(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to execute api '" + componentName + "' " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax call to execute custom callback url
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns response from the execution of callback url</returns>
        [Route("WebApi")]
        [HttpPost]
        public async Task<string> WebApi(QuickProcess.Model.api_Model request)
        {
            string componentName = "";
            try
            {
                componentName = request.ComponentName.ToString();
            }
            catch { }

            try
            {
                var resp = await QuickProcess.QuickProcess_Core.WebApi(request, getBaseUrl());
                return JsonSerializer.Serialize(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to execute api '" + componentName + "' " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax call to get list of records to bind to autocomplete, checklist and select input
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns data extracted from component datasource to be bound to input</returns>
        [Route("getDropDownList")]
        [HttpPost]
        public async Task<string> getDropDownList(QuickProcess.Model.getDropDownList_Model request)
        {
            try
            {
                var resp = await QuickProcess.QuickProcess_Core.getDropDownList(request, getBaseUrl());
                return JsonSerializer.Serialize(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to get list items " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ajax request to delete record from a component datasource
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Retruns status of operation</returns>
        [Route("deleteRecord")]
        [HttpPost]
        public async Task<string> deleteRecord(QuickProcess.Model.deleteRecord_Model request)
        {
            try
            {
                var resp = QuickProcess.QuickProcess_Core.deleteRecord(request, getBaseUrl());
                return JsonSerializer.Serialize(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to delete record " + ex.Message });
            }
        }
         

        /// <summary>
        /// This method handle http request to refresh cached project information that has been updated
        /// </summary>
        /// <returns>Returns details of current project defined through QuickProcess</returns>
        [Route("refreshFramework")]
        [HttpGet]
        public async Task refreshFramework()
        {
            QuickProcess.QuickProcess_Core.refreshFramework();
        }

        /// <summary>
        /// This method handles ajax request to get list of all components defined in current project, so that component set as prefetch will be downloaded to browser before hand
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns list of all componenets for current project</returns>
        [Route("getApplicationSettings")]
        [HttpPost]
        public async Task<string> getApplicationSettings(QuickProcess.Model.api_Model request)
        {
            try
            {
                return await QuickProcess.QuickProcess_Core.getApplicationSettings(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to get Application List " + ex.Message });
            }
        }


        /// <summary>
        /// This method handles ahax request to get list of all modules defined for current project
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Returns list of modules for current project</returns>
        [Route("getApplicationModules")]
        [HttpPost]
        public async Task<string> getApplicationModules(QuickProcess.Model.api_Model request)
        {
            try
            {
                return JsonSerializer.Serialize(await QuickProcess.QuickProcess_Core.getApplicationModules());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return JsonSerializer.Serialize(new QuickProcess.GenericResponse() { ResponseCode = "-1", ResponseDescription = "Unable to get Application List " + ex.Message });
            }
        }


        private string getBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host.Value}/";
        }

    }
}
