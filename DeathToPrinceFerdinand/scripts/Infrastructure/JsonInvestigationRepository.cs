using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using DeathToPrinceFerdinand.scripts.Core.Contradictions;
using DeathToPrinceFerdinand.scripts.Core.Models;

namespace DeathToPrinceFerdinand.scripts.Infrastructure
{
    public class JsonInvestigationRepository : IInvestigationRepository
    {
        private readonly string _dataPath;
        private readonly JsonSerializerOptions _jsonOptions;

        private List<Evidence> _evidenceCache = new();
        private List<TestimonyStatement> _testimonyCache = new();
        private List<DossierState> _dossierCache = new();
        private List<ContradictionResult> _contradictionCache = new();
        private bool _isLoaded = false;

        public JsonInvestigationRepository(string dataPath = "Data")
        {
            _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        private async Task EnsureLoadedAsync()
        {
            if (_isLoaded)
            {
                return;
            }

            LoadAllData();
            _isLoaded = true;

            await Task.CompletedTask;
        }

        private void LoadAllData()
        {
            LoadAllEvidence();
            LoadAllTestimonies();
            LoadAllDossiers();
        }

        private void LoadAllEvidence()
        {
            var evidencePath = $"{_dataPath}/evidence.json";

            if (FileAccess.FileExists(evidencePath))
            {
                var file = FileAccess.Open(evidencePath, FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    var evidenceJson = file.GetAsText();
                    file.Close();

                    var evidenceData = JsonSerializer.Deserialize<List<Evidence>>(evidenceJson, _jsonOptions);
                    _evidenceCache = evidenceData ?? new List<Evidence>();
                }
                else
                {
                    GD.PrintErr($"X Failed to open evidence file.");
                    _evidenceCache = new List<Evidence>();
                }
            }
            else
            {
                GD.PrintErr($"X Evidence file does not exist: {evidencePath}");
                _evidenceCache = new List<Evidence>();
            }
        }

        private void LoadAllTestimonies()
        {
            var testimonyPath = $"{_dataPath}/testimony.json";
            GD.Print($"[Repo] Loading: {testimonyPath} (abs: {ProjectSettings.GlobalizePath(testimonyPath)}");

            if (FileAccess.FileExists(testimonyPath))
            {
                var file = FileAccess.Open(testimonyPath, FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    var testimonyJson = file.GetAsText();
                    file.Close();

                    var testimonyData = JsonSerializer.Deserialize<List<TestimonyStatement>>(testimonyJson, _jsonOptions);
                    _testimonyCache = testimonyData ?? new List<TestimonyStatement>();
                }
                else
                {
                    GD.PrintErr($"X Failed to open testimony file");
                    _testimonyCache = new List<TestimonyStatement>();
                }
            }
            else
            {
                GD.PrintErr($"X Testimony file does not exist: {testimonyPath}");
                _testimonyCache = new List<TestimonyStatement>();
            }
        }

        private void LoadAllDossiers()
        {
            var dossiersPath = $"{_dataPath}/dossiers.json";

            if (FileAccess.FileExists(dossiersPath))
            {
                var file = FileAccess.Open(dossiersPath, FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    var dossiersJson = file.GetAsText();
                    file.Close();

                    var dossiersData = JsonSerializer.Deserialize<List<DossierState>>(dossiersJson, _jsonOptions);
                    _dossierCache = dossiersData ?? new List<DossierState>();
                }
                else
                {
                    GD.PrintErr($"X Failed to open dossiers file");
                    _dossierCache = new List<DossierState>();
                }
            }
            else
            {
                GD.PrintErr($"X Dossiers file does not exist: {dossiersPath}");
                _dossierCache = new List<DossierState>();
            }
        }

        public async Task<Evidence?> GetEvidenceAsync(string id)
        {
            await EnsureLoadedAsync();
            return _evidenceCache.FirstOrDefault(e => e.Id == id);
        }

        public async Task<IEnumerable<Evidence>> GetAllEvidenceAsync()
        {
            await EnsureLoadedAsync();
            return _evidenceCache.AsReadOnly();
        }

        public async Task<TestimonyStatement?> GetTestimonyAsync(string id)
        {
            await EnsureLoadedAsync();
            return _testimonyCache.FirstOrDefault(t => t.Id == id);
        }

        public async Task<IEnumerable<TestimonyStatement>> GetAllTestimonyAsync()
        {
            await EnsureLoadedAsync();
            return _testimonyCache.AsReadOnly();
        }

        public async Task<DossierState?> GetDossierAsync(string suspectId)
        {
            await EnsureLoadedAsync();
            return _dossierCache.FirstOrDefault(d => d.SuspectId == suspectId);
        }

        public async Task<IEnumerable<DossierState>> GetAllDossiersAsync()
        {
            await EnsureLoadedAsync();
            return _dossierCache.AsReadOnly();
        }

        public async Task SaveEvidenceAsync(Evidence evidence)
        {
            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            await EnsureLoadedAsync();

            var existing = _evidenceCache.FirstOrDefault(e => e.Id == evidence.Id);
            if (existing != null)
            {
                var index = _evidenceCache.IndexOf(existing);
                _evidenceCache[index] = evidence;
            }
            else
            {
                _evidenceCache.Add(evidence);
            }

            await SaveEvidenceFileAsync();
        }

        public async Task SaveTestimonyAsync(TestimonyStatement testimony)
        {
            if (testimony == null)
            {
                throw new ArgumentNullException(nameof(testimony));
            }

            await EnsureLoadedAsync();

            var existing = _testimonyCache.FirstOrDefault(t => t.Id == testimony.Id);
            if (existing != null)
            {
                var index = _testimonyCache.IndexOf(existing);
                _testimonyCache[index] = testimony;
            }
            else
            {
                _testimonyCache.Add(testimony);
            }

            await SaveTestimonyFileAsync();
        }

        public async Task SaveDossierAsync(DossierState dossier)
        {
            if (dossier == null)
            {
                throw new ArgumentNullException(nameof(dossier));
            }

            await EnsureLoadedAsync();

            dossier.LastUpdated = DateTime.UtcNow;

            var existing = _dossierCache.FirstOrDefault(d => d.SuspectId == dossier.SuspectId);
            if (existing != null)
            {
                var index = _dossierCache.IndexOf(existing);
                _dossierCache[index] = dossier;
            }
            else
            {
                _dossierCache.Add(dossier);
            }

            await SaveDossiersFileAsync();
        }

        public async Task SaveContradictionAsync(ContradictionResult contradiction)
        {
            if (contradiction == null)
            {
                throw new ArgumentNullException(nameof(contradiction));
            }

            await EnsureLoadedAsync();

            var existing = _contradictionCache.FirstOrDefault(c => c.ContradictionId == contradiction.ContradictionId);
            if (existing != null)
            {
                var index = _contradictionCache.IndexOf(existing);
                _contradictionCache[index] = contradiction;
            }
            else
            {
                _contradictionCache.Add(contradiction);
            }

            await SaveContradictionsFileAsync();
        }

        private Task SaveEvidenceFileAsync()
        {
            return Task.CompletedTask;
        }

        private Task SaveTestimonyFileAsync()
        {
            return Task.CompletedTask;
        }

        private Task SaveDossiersFileAsync()
        {
            return Task.CompletedTask;
        }

        private Task SaveContradictionsFileAsync()
        {
            return Task.CompletedTask;
        }
    }
}
