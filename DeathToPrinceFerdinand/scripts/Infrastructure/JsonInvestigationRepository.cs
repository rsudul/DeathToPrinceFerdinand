using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }

        private async Task EnsureLoadedAsync()
        {
            if (_isLoaded)
            {
                return;
            }

            await LoadAllDataAsync();
            _isLoaded = true;
        }

        private async Task LoadAllDataAsync()
        {
            var evidencePath = Path.Combine(_dataPath, "evidence.json");
            if (File.Exists(evidencePath))
            {
                var evidenceJson = await File.ReadAllTextAsync(evidencePath);
                var evidenceData = JsonSerializer.Deserialize<List<Evidence>>(evidenceJson, _jsonOptions);
                _evidenceCache = evidenceData ?? new List<Evidence>();
            }

            var testimonyPath = Path.Combine(_dataPath, "testimony.json");
            if (File.Exists(testimonyPath))
            {
                var testimonyJson = await File.ReadAllTextAsync(testimonyPath);
                var testimonyData = JsonSerializer.Deserialize<List<TestimonyStatement>>(testimonyJson, _jsonOptions);
                _testimonyCache = testimonyData ?? new List<TestimonyStatement>();
            }

            var dossiersPath = Path.Combine(_dataPath, "dossiers.json");
            if (File.Exists(dossiersPath))
            {
                var dossiersJson = await File.ReadAllTextAsync(dossiersPath);
                var dossiersData = JsonSerializer.Deserialize<List<DossierState>>(dossiersJson, _jsonOptions);
                _dossierCache = dossiersData ?? new List<DossierState>();
            }

            var contradictionsPath = Path.Combine(_dataPath, "contradictions.json");
            if (File.Exists(contradictionsPath))
            {
                var contradictionsJson = await File.ReadAllTextAsync(contradictionsPath);
                var contradictionsData = JsonSerializer.Deserialize<List<ContradictionResult>>(contradictionsJson, _jsonOptions);
                _contradictionCache = contradictionsData ?? new List<ContradictionResult>();
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

        private async Task SaveEvidenceFileAsync()
        {
            var evidencePath = Path.Combine(_dataPath, "evidence.json");
            var json = JsonSerializer.Serialize(_evidenceCache, _jsonOptions);
            await File.WriteAllTextAsync(evidencePath, json);
        }

        private async Task SaveTestimonyFileAsync()
        {
            var testimonyPath = Path.Combine(_dataPath, "testimony.json");
            var json = JsonSerializer.Serialize(_testimonyCache, _jsonOptions);
            await File.WriteAllTextAsync(testimonyPath, json);
        }

        private async Task SaveDossiersFileAsync()
        {
            var dossiersPath = Path.Combine(_dataPath, "dossiers.json");
            var json = JsonSerializer.Serialize(_dossierCache, _jsonOptions);
            await File.WriteAllTextAsync(dossiersPath, json);
        }

        private async Task SaveContradictionsFileAsync()
        {
            var contradictionsPath = Path.Combine(_dataPath, "contradictions.json");
            var json = JsonSerializer.Serialize(_contradictionCache, _jsonOptions);
            await File.WriteAllTextAsync(contradictionsPath, json);
        }
    }
}
