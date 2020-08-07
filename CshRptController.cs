
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole;
using Serilog.Sinks.File;
using Serilog.Sinks.RollingFile;
using Npgsql;
//using Npgsql.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
namespace McrSrv.Controllers
{
    [Route("rpts/[controller]")]
    public class CshRptController : ControllerBase
    {
        NpgsqlConnection conn;
        private readonly ILogger<CshRptController> _logger;
        private IConfiguration _configuration;
        public CshRptController(ILogger<CshRptController> logger,IConfiguration Configuration)
        {
            _logger=logger;
            _configuration = Configuration;
            

        }
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            
            return new string[] { "", "" };
        }

        // GET rpts/cshrpt/ath
        [HttpGet("ath")]
        
        public string Get(string tkn,string argsr)
        {
            NpgsqlDataAdapter da=new NpgsqlDataAdapter();
            DataTable dt=new DataTable();
            DataTable cdt=new DataTable("cshttl");
            DataTable cddt=new DataTable("cshdet");;
            DataSet argsds;
            DataSet csrpds=new DataSet();
            
            string rst="";
            if (argsr==null)
            return "error";
            try {
            argsds=JsonConvert.DeserializeObject<DataSet>(argsr);
            }
            catch (System.Exception ex)
                    {
                        _logger.LogError("error : Ctrl-CshRpt,mthd-Get. Error parse args: {errmsg}", ex.Message);
                        return "error: "+ex.Message;
                    }

            NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder();
            var cnf=_configuration["DBConn:DBHost"];
            csb.Host=cnf.ToString();
           
            csb.Port=6432;
            csb.Username="micro";
            
            csb.Database="micro";
            
            csb.CommandTimeout=1200;
            csb.MaxPoolSize=500;
            csb.Timeout=180;
            cnf=_configuration["DBConn:DBPass"];
            csb.Password=cnf.ToString();
            
            string fst=csb.ConnectionString;
            
            conn = new NpgsqlConnection(fst);
            conn.Open();
            NpgsqlCommand nsc=new NpgsqlCommand("SELECT * FROM users WHERE name=split_part('"+tkn+"',':',1) AND pass=split_part('"+tkn+"',':',2)",conn);
            rst="";
            da.SelectCommand=nsc;
            da.Fill(dt);
            if (dt.Rows.Count>0)
            {
                if (((bool)dt.Rows[0]["super"]) || ((bool)dt.Rows[0]["boss"]) || (Convert.ToInt32(dt.Rows[0]["id"])==921))
                {
                    nsc = new NpgsqlCommand("SELECT * FROM vw_dt_cash('"+Convert.ToDateTime(argsds.Tables[0].Rows[0]["bdt"]).ToShortDateString()+"','"+
                    Convert.ToDateTime(argsds.Tables[0].Rows[0]["edt"]).ToShortDateString()+"')  ORDER BY cdate desc,posid", conn);
                    da.SelectCommand = nsc;
                    da.Fill(cdt);
                    nsc = new NpgsqlCommand("select * from get_cshr_det('"+Convert.ToDateTime(argsds.Tables[0].Rows[0]["bdt"]).ToShortDateString()+"','"+
                    Convert.ToDateTime(argsds.Tables[0].Rows[0]["edt"]).ToShortDateString()+"', (SELECT array_agg(id) FROM poses)) order by pid",conn);
                        da.SelectCommand = nsc;
                        da.Fill(cddt);
                }
                else
                {
                    if (((bool)dt.Rows[0]["posadm"]))
                    {
                        nsc = new NpgsqlCommand("set session datestyle to 'Euro,DMY'; SELECT * FROM vw_dt_cash('"+Convert.ToDateTime(argsds.Tables[0].Rows[0]["bdt"]).ToShortDateString()+"','"+
                        Convert.ToDateTime(argsds.Tables[0].Rows[0]["edt"]).ToShortDateString()+"') WHERE posid IN (" + dt.Rows[0]["psls"].ToString() + ") ORDER BY cdate desc,posid", conn);
                        da.SelectCommand = nsc;
                        da.Fill(cdt);
                        nsc = new NpgsqlCommand("set session datestyle to 'Euro,DMY'; select * from get_cshr_det('"+Convert.ToDateTime(argsds.Tables[0].Rows[0]["bdt"]).ToShortDateString()+"','"+
                        Convert.ToDateTime(argsds.Tables[0].Rows[0]["edt"]).ToShortDateString()+"',array["+dt.Rows[0]["psls"].ToString()+"]) order by pid",conn);
                        da.SelectCommand = nsc;
                        da.Fill(cddt);
                    }
                    else
                    {
                        nsc = new NpgsqlCommand("SELECT * FROM vw_dt_cash('"+Convert.ToDateTime(argsds.Tables[0].Rows[0]["bdt"]).ToShortDateString()+"','"+
                        Convert.ToDateTime(argsds.Tables[0].Rows[0]["edt"]).ToShortDateString()+"') WHERE posid =" + dt.Rows[0]["posid"].ToString() + " ORDER BY cdate desc,posid", conn);
                        da.SelectCommand = nsc;
                        da.Fill(cdt);
                        nsc = new NpgsqlCommand("select * from get_cshr_det('"+Convert.ToDateTime(argsds.Tables[0].Rows[0]["bdt"]).ToShortDateString()+"','"+
                        Convert.ToDateTime(argsds.Tables[0].Rows[0]["edt"]).ToShortDateString()+"',array["+dt.Rows[0]["posid"].ToString()+"]) order by pid",conn);
                        da.SelectCommand = nsc;
                        da.Fill(cddt);
                    }
                }
                CultureInfo current = CultureInfo.CurrentCulture;
                CultureInfo frc;
                frc = new CultureInfo("ru-RU");
                csrpds.Tables.AddRange(new DataTable[]{cdt,cddt});
                
                JsonSerializerSettings microsoftDateFormatSettings = new JsonSerializerSettings
                    {
                
                        DateFormatHandling = DateFormatHandling.IsoDateFormat 
                    };

                rst=JsonConvert.SerializeObject(csrpds,new IsoDateTimeConverter { DateTimeFormat = "dd.MM.yyyy" });
            _logger.LogInformation("Method CshRpt-Get {tkn}. Success", tkn.Split(':')[0]);
            }
            else
            {
            _logger.LogInformation("Method CshRpt-Get {tkn}. No such sucker", tkn.Split(':')[0]);
            rst="error";
            }
            conn.Close();
            return rst;
        }
        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
