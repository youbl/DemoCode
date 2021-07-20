using System.Collections.Generic;
using System.Web.Http;

namespace DemoCodeWeb.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/<controller>
        [Route("aaa")]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
        [Route("bbb")]
        public IEnumerable<string> Get2()
        {
            return new string[] { "value11", "value22" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}