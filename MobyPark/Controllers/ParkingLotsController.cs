using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("")]
    [Authorize]
    public class ParkingLotsController : ControllerBase
    {
        private readonly IHostEnvironment _env;

        public ParkingLotsController(IHostEnvironment env)
        {
            _env = env;
        }

        // GET /parking-lots
        [HttpGet("parking-lots")]
        public IActionResult GetAll()
        {
            var data = LoadParkingLotData();
            return Ok(data);
        }

        // GET /parking-lots/{lid}
        [HttpGet("parking-lots/{lid}")]
        public IActionResult GetById(string lid)
        {
            var data = LoadParkingLotData();
            if (data == null || !data.ContainsKey(lid))
                return NotFound("Parking lot not found");

            return Ok(data[lid]);
        }

        // GET /parking-lots/{lid}/sessions
        [HttpGet("parking-lots/{lid}/sessions")]
        public IActionResult GetSessions(string lid)
        {
            var data = LoadParkingLotData();
            if (data == null || !data.ContainsKey(lid))
                return NotFound("Parking lot not found");

            var sessions = LoadParkingLotSessions(lid);
            if (sessions is null)
                return NotFound("Sessions not found");

            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.Equals(role, "ADMIN", System.StringComparison.OrdinalIgnoreCase))
            {
                // admin: return the full sessions object (dictionary)
                return Ok(sessions);
            }

            // non-admin: return list of session objects that belong to this username
            var list = new List<JsonElement>();
            foreach (var kv in sessions)
            {
                try
                {
                    if (kv.Value.ValueKind == JsonValueKind.Object &&
                        kv.Value.TryGetProperty("user", out var userProp) &&
                        userProp.GetString() == username)
                    {
                        list.Add(kv.Value.Clone());
                    }
                }
                catch
                {
                    // ignore malformed session entries
                }
            }

            return Ok(list);
        }

        // GET /parking-lots/{lid}/sessions/{sid}
        [HttpGet("parking-lots/{lid}/sessions/{sid}")]
        public IActionResult GetSessionById(string lid, string sid)
        {
            var data = LoadParkingLotData();
            if (data == null || !data.ContainsKey(lid))
                return NotFound("Parking lot not found");

            var sessions = LoadParkingLotSessions(lid);
            if (sessions is null)
                return NotFound("Sessions not found");

            if (!sessions.ContainsKey(sid))
                return NotFound("Session not found");

            var session = sessions[sid];
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.Equals(role, "ADMIN", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!session.TryGetProperty("user", out var userProp) || userProp.GetString() != username)
                {
                    return StatusCode(403, "Access denied");
                }
            }

            return Ok(session);
        }

        private Dictionary<string, JsonElement>? LoadParkingLotData()
        {
            // Try a few sensible locations where the test/server might store parking-lots
            var candidates = new[] {
                Path.Combine(_env.ContentRootPath, "data", "parking-lots.json"),
                Path.Combine(_env.ContentRootPath, "..", "Server", "data", "parking-lots.json")
            };

            foreach (var path in candidates)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(path);
                        var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            var dict = new Dictionary<string, JsonElement>();
                            foreach (var prop in doc.RootElement.EnumerateObject())
                                dict[prop.Name] = prop.Value.Clone();
                            return dict;
                        }
                    }
                    catch
                    {
                        // ignore parse errors and continue
                    }
                }
            }

            // If no file found, return empty dictionary to avoid nulls for callers
            return new Dictionary<string, JsonElement>();
        }

        private Dictionary<string, JsonElement>? LoadParkingLotSessions(string lid)
        {
            var candidates = new[] {
                Path.Combine(_env.ContentRootPath, "data", "pdata", $"p{lid}-sessions.json"),
                Path.Combine(_env.ContentRootPath, "..", "Server", "data", "pdata", $"p{lid}-sessions.json")
            };

            foreach (var path in candidates)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(path);
                        var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            var dict = new Dictionary<string, JsonElement>();
                            foreach (var prop in doc.RootElement.EnumerateObject())
                                dict[prop.Name] = prop.Value.Clone();
                            return dict;
                        }
                    }
                    catch
                    {
                        // ignore parse errors and continue
                    }
                }
            }

            return null;
        }
    }
}
