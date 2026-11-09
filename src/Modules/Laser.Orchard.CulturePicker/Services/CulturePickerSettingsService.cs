using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.ContentManagement;
using Laser.Orchard.CulturePicker.Models;
using Orchard.Caching;
using Orchard.Data;
using Orchard.Localization.Services;
using Orchard;
using Orchard.Services;

namespace Laser.Orchard.CulturePicker.Services {
    public class CulturePickerSettingsService : ICulturePickerSettingsService {
        private const string ExtendedCulturesCacheKey = "Laser.Orchard.CulturePicker.ExtendedCultures";
        private const string ExtendedCulturesChangedSignal = "Laser.Orchard.CulturePicker.ExtendedCulturesChanged";
        // the cache is per node: the expiry bounds how long the other nodes keep a stale list after a change
        private static readonly TimeSpan ExtendedCulturesCacheDuration = TimeSpan.FromMinutes(5);

        private readonly IContentManager _contentManager;
        private readonly ICultureManager _cultureManager;
        private readonly IWorkContextAccessor _work;
        private readonly ILocalizationService _localizationService;
        private readonly IRepository<ExtendedCultureRecord> _extendedCultureRepository;
        private readonly IRepository<SettingsRecord> _settingsRepository;
        private readonly ICacheManager _cacheManager;
        private readonly ISignals _signals;
        private readonly IClock _clock;


        public CulturePickerSettingsService(ILocalizationService localizationService, ICultureManager cultureManager, IContentManager contentManager, IRepository<ExtendedCultureRecord> repository, IWorkContextAccessor work, IRepository<Models.SettingsRecord> settingsRepository,
            ICacheManager cacheManager, ISignals signals, IClock clock) {
            _localizationService = localizationService;
            _cultureManager = cultureManager;
            _contentManager = contentManager;
            _extendedCultureRepository = repository;
            _settingsRepository = settingsRepository;
            _work = work;
            _cacheManager = cacheManager;
            _signals = signals;
            _clock = clock;
        }

        public CulturePicker.Models.SettingsModel ReadSettings() {
            var settingsRecord = _settingsRepository.Table.FirstOrDefault();
            if (settingsRecord == null) {
                // use the new record directly: on read requests the session is flushed only at commit,
                // so a query here would not see it yet
                settingsRecord = new CulturePicker.Models.SettingsRecord();
                settingsRecord.ShowOnlyPertinentCultures = false;
                _settingsRepository.Create(settingsRecord);
            }
            var settings = new Models.SettingsModel();
            settings.Settings = settingsRecord;
            settings.ExtendedCulturesList = this.CultureList(null, true);
            return settings;
        }

        public void WriteSettings(CulturePicker.Models.SettingsModel settings) {
            foreach (var culture in settings.ExtendedCulturesList) {
                this.UpdateCulture(culture);
            }
            _settingsRepository.Update(settings.Settings);
        }

        public IList<ExtendedCultureRecord> CultureList(IEnumerable<string> cultureListSource = null, bool ordered = true) {
            List<string> listCultures;
            if (cultureListSource == null)
                listCultures = _cultureManager.ListCultures().ToList();
            else {
                listCultures = cultureListSource.ToList();
            }
            var siteCulture = _cultureManager.GetSiteCulture();
            var listExtendedCultures = GetExtendedCultureRows();
            var joinedCultures = from c in listCultures
                                 join
                                     ec in listExtendedCultures on c.ToString() equals ec.CultureCode into temp_ec
                                 from ec in temp_ec.DefaultIfEmpty()
                                 select new ExtendedCultureRecord { CultureCode = c.ToString(), Id = (ec != null ? ec.Id : 0), Priority = (ec != null ? ec.Priority : (c.ToString() == siteCulture ? 0 : 1)), DisplayName = (ec != null ? ec.DisplayName : null) };

            if (ordered)
                return (joinedCultures.OrderBy(o => o.Priority).ToList());
            else
                return (joinedCultures.ToList());

        }

        public void UpdateCulture(ExtendedCultureRecord culture) {
            _extendedCultureRepository.Flush();
            var record = _extendedCultureRepository.Get(o => o.CultureCode.Equals(culture.CultureCode));
            if (record != null) {
                //record.CultureCode = culture.CultureCode;
                //record.Priority = culture.Priority;
                //record.DisplayName = culture.DisplayName != "" ? culture.DisplayName : null;
                _extendedCultureRepository.Update(culture);
            } else {
                //record = new ExtendedCultureRecord();
                //record.CultureCode = culture.CultureCode;
                //record.DisplayName = culture.DisplayName != "" ? culture.DisplayName : null;
                //record.Priority = culture.Priority;
                _extendedCultureRepository.Create(culture);
            }
            _signals.Trigger(ExtendedCulturesChangedSignal);

            record = null;
        }

        public ExtendedCultureRecord GetExtendedCulture(string culture) {
            // the former SQL query compared with the database collation (case-insensitive)
            var row = GetExtendedCultureRows().FirstOrDefault(o => string.Equals(o.CultureCode, culture, StringComparison.OrdinalIgnoreCase));
            return row == null ? null : Copy(row);
        }

        /// <summary>
        /// Extended culture records of the tenant, cached. The cached objects are never handed out:
        /// callers get copies, so changing them does not alter the cache.
        /// </summary>
        private IList<ExtendedCultureRecord> GetExtendedCultureRows() {
            return _cacheManager.Get(ExtendedCulturesCacheKey, true, ctx => {
                ctx.Monitor(_signals.When(ExtendedCulturesChangedSignal));
                ctx.Monitor(_clock.When(ExtendedCulturesCacheDuration));
                return _extendedCultureRepository.Table.ToList().Select(Copy).ToList();
            });
        }

        private static ExtendedCultureRecord Copy(ExtendedCultureRecord source) {
            return new ExtendedCultureRecord {
                Id = source.Id,
                CultureCode = source.CultureCode,
                DisplayName = source.DisplayName,
                Priority = source.Priority
            };
        }
    }
}
