// Global variables defined here
let allData = [];
let groupedData = {};
let pendingChanges = new Map();
let originalValues = new Map(); // Track original values for change detection
let toastTimeout;
let autoSaveTimeout; // Timer for autosave
let isAdmin = false;
let currentPage = 1;
const recordsPerPage = 10;
let employeesLists = [];
let isShowingSearchResults = false; // Track if we're showing search results
let showProcessedCompanies = false; // Track whether to show processed companies
const AUTO_SAVE_DELAY = 2000; // Auto-save after 2 seconds of inactivity

// Language and Translation System
let currentLanguage = 'en'; // Default to English

const translations = {
  en: {
    dashboard_title: "Lønservice Monitoring Dashboard",
    global_search_placeholder: "Global search across all data...",
    clear_filters: "Clear Filters",
    expand_all: "Expand All",
    collapse_all: "Collapse All",
    loading: "Loading...",
    loading_dashboard: "Loading dashboard data...",
    admin_panel: "Admin Panel",
    username: "Username",
    password: "Password",
    login: "Login",
    show_columns: "Show Columns",
    hide_columns: "Hide Columns",
    save: "Save",
    close: "Close",
    search: "Search",
    history: "History",
    status: "Status",
    assignee: "Assignee",
    notes: "Notes",
    company: "Company",
    total_count: "Total Rows",
    processed_count: "Processed",
    pending_count: "Pending Count",
    error_count: "Error Count",
    no_data: "No Data Available",
    save_changes: "Save Changes",
    refresh: "Refresh",
    export: "Export",
    filter: "Filter",
    all: "All",
    select_employee: "Select Employee",
    select_status: "Select Status",
    view_company_history: "View Company History",
    not_started: "Not Started",
    in_progress: "In Progress",
    waiting: "Waiting",
    processed: "Processed",
    yes: "Yes",
    no: "No",
    unknown: "Unknown",
    close: "Close",
    // History modal
    history: "History",
    history_for_company: "History for Company",
    no_history_records: "No history records found for this company.",
    date_time: "Date/Time",
    action: "Action",
    field: "Field", 
    old_value: "Old Value",
    new_value: "New Value",
    modified_by: "Modified By",
    none: "None",
    // Notes column and input
    notes_column: "Notes",
    add_notes_placeholder: "Add notes...",
    // Common Notes values translations
    notes_pending: "Pending",
    notes_completed: "Completed",
    notes_in_review: "In Review",
    notes_approved: "Approved",
    notes_rejected: "Rejected",
    notes_follow_up: "Follow up required",
    notes_urgent: "Urgent",
    notes_resolved: "Resolved",
    notes_cancelled: "Cancelled",
    notes_on_hold: "On Hold",
    // Column headers (including Danish originals)
    firmanr: "Company Number",
    notes: "Notes",
    createdate: "Created Date",
    modifieddate: "Modified Date",
    assigneename: "Assignee Name",
    processedstatus: "Processed Status",
    timeblock: "Time Block",
    confirmed: "Confirmed",
    sourcefilename: "Source File Name",
    companyassignee: "Company Assignee",
    companyassigneename: "Company Assignee Name",
    koncernnr_: "Group Number",
    medarbejdernr: "Employee Number",
    listenummer: "List Number",
    l_nart: "Wage Type",
    sats: "Rate",
    // Form controls and labels
    search_within_company: "Search within",
    clear_group_filters: "Clear filters for this group",
    showing_records: "Showing",
    of_records: "of",
    records: "records",
    page_size: "Page size",
    previous: "Previous",
    next: "Next",
    page: "Page",
    select_page_size: "Select page size",
    // Status options
    status_not_started: "Not Started",
    status_in_progress: "In Progress", 
    status_waiting: "Waiting",
    status_processed: "Processed",
    // Buttons and actions
    expand_group: "Expand group",
    collapse_group: "Collapse group",
    // Form controls and labels
    search_within_company: "Search within {company}...",
    filter_placeholder: "Filter {column}...",
    showing_records: "Showing",
    of_records: "of",
    records: "records",
    records_per_page: "records per page",
    page: "Page",
    previous: "Previous",
    next: "Next",
    // Status options in English
    not_started_status: "Not Started",
    in_progress_status: "In Progress",
    waiting_status: "Waiting", 
    processed_status: "Processed",
    // Common form elements
    select_option: "Select...",
    search_placeholder: "Search...",
    no_results_found: "No results found",
    loading_data: "Loading data...",
    error_loading_data: "Error loading data",
    // Actions
    save_changes: "Save Changes",
    cancel: "Cancel",
    delete: "Delete",
    edit: "Edit",
    view: "View",
    add: "Add",
    remove: "Remove",
    // Pagination
    first_page: "First page",
    last_page: "Last page",
    items_per_page: "Items per page"
  },
  da: {
    dashboard_title: "Lønservice Overvågning Dashboard",
    global_search_placeholder: "Global søgning på tværs af alle data...",
    clear_filters: "Ryd Filtre",
    expand_all: "Udvid Alle",
    collapse_all: "Sammenfold Alle",
    loading: "Indlæser...",
    loading_dashboard: "Indlæser dashboard data...",
    admin_panel: "Admin Panel",
    username: "Brugernavn",
    password: "Adgangskode",
    login: "Log ind",
    show_columns: "Vis Kolonner",
    hide_columns: "Skjul Kolonner",
    save: "Gem",
    close: "Luk",
    search: "Søg",
    history: "Historie",
    status: "Status",
    assignee: "Tildelt",
    notes: "Noter",
    company: "Virksomhed",
    total_count: "Samlede Rækker",
    processed_count: "Behandlet",
    pending_count: "Afventende Antal",
    error_count: "Fejl Antal",
    no_data: "Ingen Data Tilgængelig",
    save_changes: "Gem Ændringer",
    refresh: "Opdater",
    export: "Eksporter",
    filter: "Filter",
    all: "Alle",
    select_employee: "Vælg Medarbejder",
    select_status: "Vælg Status",
    view_company_history: "Se Virksomhedshistorik",
    not_started: "Ikke Startet",
    in_progress: "I Gang",
    waiting: "Venter",
    processed: "Behandlet",
    yes: "Ja",
    no: "Nej",
    unknown: "Ukendt",
    close: "Luk",
    // History modal
    history: "Historik",
    history_for_company: "Historik for Virksomhed",
    no_history_records: "Ingen historikposter fundet for denne virksomhed.",
    date_time: "Dato/Tid",
    action: "Handling",
    field: "Felt", 
    old_value: "Gammel Værdi",
    new_value: "Ny Værdi",
    modified_by: "Ændret Af",
    none: "Ingen",
    // Notes column and input
    notes_column: "Noter",
    add_notes_placeholder: "Tilføj noter...",
    // Common Notes values translations in Danish
    notes_pending: "Afventende",
    notes_completed: "Afsluttet",
    notes_in_review: "Under Gennemgang",
    notes_approved: "Godkendt",
    notes_rejected: "Afvist",
    notes_follow_up: "Opfølgning Påkrævet",
    notes_urgent: "Hastende",
    notes_resolved: "Løst",
    notes_cancelled: "Annulleret", 
    notes_on_hold: "På Standby",
    // Column headers (including Danish originals)
    firmanr: "Firmanummer",
    notes: "Noter",
    createdate: "Oprettelsesdato",
    modifieddate: "Ændringsdato",
    assigneename: "Tildelt Navn",
    processedstatus: "Behandlingsstatus",
    timeblock: "Tidsblok",
    confirmed: "Bekræftet",
    sourcefilename: "Kildefil Navn",
    companyassignee: "Virksomhedstildelt",
    companyassigneename: "Virksomhedstildelt Navn",
    koncernnr_: "Koncernnummer",
    medarbejdernr: "Medarbejdernummer",
    listenummer: "Listenummer",
    l_nart: "Lønart",
    sats: "Sats",
    // Form controls and labels
    search_within_company: "Søg inden for",
    clear_group_filters: "Ryd filtre for denne gruppe",
    showing_records: "Viser",
    of_records: "af",
    records: "poster",
    page_size: "Sidestørrelse",
    previous: "Forrige",
    next: "Næste", 
    page: "Side",
    select_page_size: "Vælg sidestørrelse",
    // Status options
    status_not_started: "Ikke Startet",
    status_in_progress: "I Gang",
    status_waiting: "Venter",
    status_processed: "Behandlet",
    // Buttons and actions
    expand_group: "Udvid gruppe",
    collapse_group: "Sammenfold gruppe",
    // Form controls and labels
    search_within_company: "Søg inden for {company}...",
    filter_placeholder: "Filter {column}...",
    showing_records: "Viser",
    of_records: "af",
    records: "poster",
    records_per_page: "poster pr. side",
    page: "Side",
    previous: "Forrige",
    next: "Næste",
    // Status options in Danish
    not_started_status: "Ikke Startet",
    in_progress_status: "I Gang", 
    waiting_status: "Venter",
    processed_status: "Behandlet",
    // Common form elements
    select_option: "Vælg...",
    search_placeholder: "Søg...",
    no_results_found: "Ingen resultater fundet",
    loading_data: "Indlæser data...",
    error_loading_data: "Fejl ved indlæsning af data",
    // Actions
    save_changes: "Gem Ændringer",
    cancel: "Annuller",
    delete: "Slet",
    edit: "Rediger",
    view: "Se",
    add: "Tilføj",
    remove: "Fjern",
    // Pagination
    first_page: "Første side",
    last_page: "Sidste side",
    items_per_page: "Elementer pr. side"
  }
};

// Language toggle function
function toggleLanguage() {
  currentLanguage = currentLanguage === 'en' ? 'da' : 'en';
  localStorage.setItem('preferredLanguage', currentLanguage);
  updateLanguageDisplay();
  translatePage();
}

// Update language button display
function updateLanguageDisplay() {
  const languageButton = document.getElementById('currentLanguage');
  if (languageButton) {
    languageButton.textContent = currentLanguage === 'en' ? 'DA' : 'EN';
  }
}

// Translate all elements on the page
function translatePage() {
  const currentTranslations = translations[currentLanguage];
  
  // Translate elements with data-translate attribute
  document.querySelectorAll('[data-translate]').forEach(element => {
    const key = element.getAttribute('data-translate');
    if (currentTranslations[key]) {
      element.textContent = currentTranslations[key];
    }
  });
  
  // Translate placeholder attributes
  document.querySelectorAll('[data-translate-placeholder]').forEach(element => {
    const key = element.getAttribute('data-translate-placeholder');
    if (currentTranslations[key]) {
      element.placeholder = currentTranslations[key];
    }
  });
  
  // Translate title attributes
  document.querySelectorAll('[data-translate-title]').forEach(element => {
    const key = element.getAttribute('data-translate-title');
    if (currentTranslations[key]) {
      element.title = currentTranslations[key];
    }
  });
  
  // Re-create global filter dropdowns to update their text
  if (typeof createCompanyFilter === 'function') {
    createCompanyFilter();
  }
  
  // Re-render the dashboard to update dynamic content
  if (allData.length > 0) {
    renderGroupedData();
  }
}

// Initialize language from localStorage
function initializeLanguage() {
  // Only load saved language if it's different from default English
  const savedLanguage = localStorage.getItem('preferredLanguage');
  if (savedLanguage && savedLanguage !== 'en' && translations[savedLanguage]) {
    currentLanguage = savedLanguage;
  } else {
    // Ensure English is default and remove any stored non-English preference
    currentLanguage = 'en';
    localStorage.removeItem('preferredLanguage');
  }
  updateLanguageDisplay();
  translatePage();
}

// Helper function to get translated text
function t(key) {
  return translations[currentLanguage][key] || key;
}

// Debounce function for performance optimization
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

// Column visibility settings
let columnVisibilitySettings = {};
let allDatabaseColumns = [];
let hiddenColumns = [];
let essentialColumns = [];
let dashboardConfig = null;
const statusOptions = ["Not Started", "In Progress", "Waiting", "Processed"];

// Helper function to get translated status label
function getTranslatedStatus(status) {
  const statusMap = {
    'Not Started': t('not_started_status'),
    'In Progress': t('in_progress_status'), 
    'Waiting': t('waiting_status'),
    'Processed': t('processed_status')
  };
  return statusMap[status] || status;
}

// Initialize dashboard
document.addEventListener("DOMContentLoaded", function () {
  initializeLanguage(); // Initialize language first
  initializeDashboard();
  setupEventListeners();

  // Check for new data every 30 seconds
  setInterval(checkForNewData, 30000);

  // Suggest incognito mode
  // if (!navigator.userAgent.includes('Headless') && !document.referrer.includes('about:blank')) {
  //     setTimeout(() => {
  //         if (confirm('For optimal security, we recommend opening this dashboard in incognito/private mode. Would you like to get instructions?')) {
  //             alert('Please:\n1. Press Ctrl+Shift+N (Chrome) or Ctrl+Shift+P (Firefox)\n2. Navigate to this URL in the new private window\n3. Close this regular window');
  //         }
  //     }, 2000);
  // }
});

// Setup event listeners
function setupEventListeners() {
  // Global search
  const globalSearchInput = document.getElementById("globalSearch");
  if (globalSearchInput) {
    globalSearchInput.addEventListener("input", debounce(performGlobalSearch, 300));
    console.log("Global search event listener attached successfully");
  } else {
    console.error("Global search input element not found!");
  }

  // Toast auto-hide after 10 seconds
  document
    .getElementById("saveToast")
    .addEventListener("shown.bs.toast", function () {
      toastTimeout = setTimeout(() => {
        saveToast.hide();
      }, 10000);
    });

  // Reset timer when toast is hidden
  document
    .getElementById("saveToast")
    .addEventListener("hidden.bs.toast", function () {
      if (toastTimeout) {
        clearTimeout(toastTimeout);
      }
    });
}

// Load dashboard configuration from server
async function loadDashboardConfiguration() {
  try {
    const response = await fetch("/api/data/configuration");
    if (!response.ok) throw new Error("Failed to fetch configuration");

    dashboardConfig = await response.json();
    hiddenColumns = dashboardConfig.systemColumns || [];
    essentialColumns = dashboardConfig.essentialColumns || [];
  } catch (error) {
    console.error("Error loading dashboard configuration:", error);
    // Fallback to default configuration
    hiddenColumns = [
      "id",
      "timeblock",
      "processedstatus",
      "companyprocessedstatus"
    ];
    essentialColumns = ["confirmed", "notes", "firmanr"];
    showError("Using default configuration due to configuration load error.");
  }
}

// Initialize dashboard
async function initializeDashboard() {
  try {
    showLoading(true);

    // Load configuration first
    await loadDashboardConfiguration();

    // Load data
    const response = await fetch("/api/data");
    if (!response.ok) throw new Error("Failed to fetch data");

    allData = await response.json();

    // Load Employee list here
    const employeeResponse = await fetch("/api/data/employees");
    if (!employeeResponse.ok) throw new Error("Failed to fetch data");
    employeesLists = await employeeResponse.json();

    createCompanyFilter();

    processAndDisplayData();
    // Show all data initially without any filters applied
    showLoading(false);
  } catch (error) {
    console.error("Error initializing dashboard:", error);
    showError("Failed to load dashboard data. Please refresh the page.");
    showLoading(false);
  }
}

// Process and display data
function processAndDisplayData() {
  console.log("=== processAndDisplayData START ===");
  console.log("allData length:", allData.length);
  console.log("allData sample:", allData.length > 0 ? allData[0] : "no data");
  
  isShowingSearchResults = false; // Reset flag when processing all data
  
  // Initialize column visibility settings first
  initializeColumnVisibility();
  
  // Group data based on configuration
  if (dashboardConfig?.grouping?.enableGrouping) {
    console.log("Grouping enabled, using groupDataByColumn");
    groupedData = groupDataByColumn(allData, dashboardConfig.grouping);
  } else {
    console.log("Grouping disabled, treating all data as one group");
    // If grouping is disabled, treat all data as one group
    groupedData = { "All Records": allData };
  }
  
  console.log("groupedData keys:", Object.keys(groupedData));
  console.log("groupedData:", groupedData);
  
  // Render grouped data
  renderGroupedData();

  // Update summary counts
  updateSummaryCounts();
  console.log("=== processAndDisplayData END ===");
}

// Group data by configurable column
function groupDataByColumn(data, groupingConfig) {
  console.log("=== groupDataByColumn START ===");
  console.log("Input data length:", data.length);
  console.log("groupingConfig:", groupingConfig);
  
  const grouped = {};
  const groupByColumn = groupingConfig.groupByColumn || 'firmanr';
  const sortByColumn = groupingConfig.sortByColumn || 'createddate';
  const sortDirection = groupingConfig.sortDirection || 'asc';

  console.log("Grouping by column:", groupByColumn);

  data.forEach((record, index) => {
    // Get the grouping value - check direct properties only
    let groupValue = record[groupByColumn] || "Unknown";
    
    console.log(`Record ${index}: ${groupByColumn} = "${groupValue}", processedStatus = "${record.processedStatus}", companyprocessedstatus = "${record.companyprocessedstatus}", assigneeName = "${record.assigneeName}", CompanyAssigneeName = "${record.CompanyAssigneeName}"`);
    
    // Handle boolean values for grouping (like 'confirmed')
    if (typeof groupValue === 'boolean') {
      groupValue = groupValue ? 'Yes' : 'No';
    }
    
    // Ensure groupValue is a string
    groupValue = String(groupValue);
    
    if (!grouped[groupValue]) {
      grouped[groupValue] = [];
    }
    grouped[groupValue].push(record);
  });

  // Sort each group by the specified column
  Object.keys(grouped).forEach((groupKey) => {
    grouped[groupKey].sort((a, b) => {
      let aVal = a[sortByColumn] || "";
      let bVal = b[sortByColumn] || "";
      
      // Handle date sorting
      if (sortByColumn.toLowerCase().includes('date')) {
        aVal = new Date(aVal);
        bVal = new Date(bVal);
      }
      
      if (sortDirection === 'desc') {
        return bVal > aVal ? 1 : -1;
      } else {
        return aVal > bVal ? 1 : -1;
      }
    });
  });

  console.log("Final grouped data:", grouped);
  console.log("Group keys:", Object.keys(grouped));
  Object.keys(grouped).forEach(key => {
    console.log(`Group "${key}": ${grouped[key].length} records`);
  });
  console.log("=== groupDataByColumn END ===");

  return grouped;
}

// Legacy function for backward compatibility
function groupDataByCompany(data) {
  const defaultGrouping = {
    groupByColumn: 'firmanr',
    sortByColumn: 'createddate',
    sortDirection: 'desc'
  };
  return groupDataByColumn(data, defaultGrouping);
}

// Render grouped data
function renderGroupedData() {
  console.log("=== renderGroupedData START ===");
  console.log("groupedData keys:", Object.keys(groupedData));
  console.log("groupedData length:", Object.keys(groupedData).length);
  
  const container = document.getElementById("dataContainer");
  container.innerHTML = "";

  if (Object.keys(groupedData).length === 0) {
    console.log("No grouped data found, showing warning message");
    container.innerHTML = `
      <div class="alert alert-warning text-center py-5 my-4" role="alert">
        <i class="fas fa-exclamation-triangle fa-3x mb-3 text-warning"></i>
        <h2 class="fw-bold mb-3">${t('no_data')}</h2>
        <p class="lead mb-0">There are currently no records to display. Please check your filters or try refreshing the page.</p>
      </div>
    `;
    return;
  }

  console.log("Rendering groups...");
  // Get all column names dynamically
  const columns = getColumnNames();
  console.log("Columns to display:", columns);
  
  Object.keys(groupedData)
    .sort()
    .forEach((company) => {
      const groupData = groupedData[company];
      const processedStatus = groupData[0]?.companyprocessedstatus || groupData[0]?.processedStatus || "Not Started";
      
      // Skip processed companies if the toggle is off
      if (!showProcessedCompanies && processedStatus === "Processed") {
        console.log(`Skipping processed group "${company}"`);
        return;
      }
      
      console.log(`Rendering group "${company}" with ${groupData.length} records`);
      const groupElement = createCompanyGroup(company, groupData, columns);
      container.appendChild(groupElement);

      // Only apply group filters if we're not showing search results
      if (!isShowingSearchResults) {
        setTimeout(() => {
          applyGroupFilters(company);
        }, 0);
      }
    });

  container.style.display = "block";
}

// Get column names dynamically
function getColumnNames() {
  if (allData.length === 0) return [];

  // Get all column names from the first record's AdditionalProperties
  const sampleRecord = allData[0];
  console.log("Sample record:", sampleRecord);
  console.log("Sample record keys:", Object.keys(sampleRecord));
  
  let allColumns = [];
  
  if (sampleRecord.additionalProperties) {
    allColumns = Object.keys(sampleRecord.additionalProperties);
    console.log("Columns from additionalProperties:", allColumns);
  } else {
    console.error("No additionalProperties found in record!");
  }

  // If column visibility is not initialized, do it now
  if (Object.keys(columnVisibilitySettings).length === 0) {
    initializeColumnVisibility();
  }

  // Filter to only visible columns
  const visibleColumns = allColumns.filter(col => {
    const isHidden = hiddenColumns.some(hidden => hidden.toLowerCase() === col.toLowerCase());
    return !isHidden;
  });
  
  console.log("Visible columns after filtering:", visibleColumns);

  // Get essential suffix columns from config (columns that should appear at the end)
  const essentialSuffixColumns = dashboardConfig.essentialColumnsSuffix || [];
  const suffixColumnsLower = essentialSuffixColumns.map(col => col.toLowerCase());
  
  // Put essential columns at the beginning, suffix columns at the end
  const specialColumns = essentialColumns.map((col) => col.toLowerCase());
  
  const remainingColumns = visibleColumns.filter(
    (col) => !specialColumns.includes(col.toLowerCase()) && 
            !suffixColumnsLower.includes(col.toLowerCase())
  );

  // Final column order: Essential columns first, then other visible columns, then suffix columns at the end
  const finalOrder = [];

  // Add essential columns in desired order if they are visible
  essentialColumns.forEach((colName) => {
    const found = visibleColumns.find(
      (col) => col.toLowerCase() === colName.toLowerCase()
    );
    if (found) finalOrder.push(found);
  });

  // Add remaining visible columns (excluding suffix columns)
  remainingColumns.forEach((col) => {
    finalOrder.push(col);
  });

  // Add suffix columns at the end in the configured order
  essentialSuffixColumns.forEach((colName) => {
    const found = visibleColumns.find(
      (col) => col.toLowerCase() === colName.toLowerCase()
    );
    if (found) finalOrder.push(found);
  });
  
  console.log("Final column order:", finalOrder);

  return finalOrder;
}
// Create Company filter
function createCompanyFilter() {
  const filterContainer = document.getElementById("dataFilter");

  const companyFilterElemnts = `
  <div class="row">
      <div class="col-6">
          <div class="input-group">
              <span class="input-group-text input-group-sm">
                  <i class="fas fa-filter"></i>
              </span>
              <select id="statusFilter" class="form-control form-select input-sm" onchange="filterCompanyBasedOnStatusnAssignee(this.value, getCurrentAssigneeFilter())">
                  <option value="" selected>${t('select_status')}</option>
                  ${statusOptions
                    .map(
                      (status) =>
                        `<option value="${status}">${getTranslatedStatus(status)}</option>`
                    )
                    .join("")}
              </select>
          </div>
      </div>
      <div class="col-6 ps-0">
          <div class="input-group">
              <span class="input-group-text input-group-sm">
                  <i class="fas fa-tags"></i>
              </span>
              <select id="assigneeFilter" class="form-control form-select input-sm" onchange="filterCompanyBasedOnStatusnAssignee(getCurrentStatusFilter(), this.value)">
                  <option value="" selected>${t('select_employee')}</option>
                  ${employeesLists
                    .map(
                      (emp) =>
                        `<option value="${emp.fullName}">${emp.fullName}</option>`
                    )
                    .join("")}
              </select>
          </div>
      </div>
  </div>`;
  if (filterContainer) {
    filterContainer.innerHTML = companyFilterElemnts;
  }
}

// Create company group
function createCompanyGroup(company, data, columns) {
  const groupDiv = document.createElement("div");
  groupDiv.className = "company-group";
  groupDiv.id = `group-${company.replace(/\s+/g, "-")}`;
  const assignee = data[0].CompanyAssigneeName || data[0].assigneeName || null;
  const hasAssignee = assignee && assignee !== "Unassigned";
  groupDiv.setAttribute("data-has-assignee", hasAssignee);

  const confirmedCount = data.filter((r) => r.confirmed).length;
  const notConfirmedCount = data.length - confirmedCount;
  const companyId = company.replace(/\s+/g, "-").toLowerCase();
  console.log("Data: ",data[0])
  const processedStatus = data[0].companyprocessedstatus || data[0].processedStatus || "Not Started";
  const totalRows = data[0].companyTotalRows || data.length;
  // Processed rows = rows with Confirmed checkbox OR rows with Notes text
  const processedRowsCount = data.filter((r) => r.confirmed || (r.notes && r.notes.trim() !== "")).length;
  const totalRowsProcessed = data[0].companyTotalRowsProcessed || processedRowsCount;
  
  groupDiv.innerHTML = `
        <div class="group-header" 
             onclick="toggleCompanyAccordion('${companyId}')"
             style="cursor: pointer;">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="d-flex gap-3 align-items-center">
                        <div style="min-width: 240px;">
                          <strong style="font-size: 1.1rem;">${company}</strong>
                        </div>
                        <span>
                          <span class="badge bg-info text-dark">
                              ${t('total_count')}: <span id="total-rows-${company}">${totalRows}</span>
                          </span>
                        </span>
                        <span>
                          <span class="badge bg-success">
                              ${t('processed_count')}: <span id="processed-rows-${company}">${totalRowsProcessed}</span>
                          </span>
                        </span>
                        <label>${t('assignee')}:</label>
                        <select id="employee-select-${companyId}" class="form-select form-select-sm bg-secondary text-white" style="width: auto; max-width: 200px;" onclick="event.stopPropagation();" onchange="handleEmployeeSelection('${company}', this.value, null)">
                            <option value="">${t('select_employee')}</option>
                            ${employeesLists
                              .map(
                                (emp) =>
                                  `<option  value="${emp.employeeID}" ${
                                    emp.fullName === assignee ? "selected" : ""
                                  }>${emp.fullName}</option>`
                              )
                              .join("")}
                        </select>
                        <label>${t('status')}:</label>
                        <select id="status-select-${companyId}" class="form-select form-select-sm bg-secondary text-white" style="width: auto; max-width: 200px;" onclick="event.stopPropagation();" onchange="handleEmployeeSelection('${company}', null, this.value)" >
                        
                        <option value="">${t('select_status')}</option>    
                        ${statusOptions
                          .map(
                            (status) => ` 
                                <option value="${status}" ${
                              processedStatus === status ? "selected" : ""
                            }>${getTranslatedStatus(status)}</option>
                            `
                          )
                          .join("")}  
                        </select>
                        <button class="btn btn-sm btn-outline-info ms-2" onclick="showCompanyHistory('${company}'); event.stopPropagation();" title="${t('view_company_history')}">
                            <i class="fas fa-history"></i> ${t('history')}
                        </button>
                    </div>
                </div>
                <div class="collapse-icon">
                    <i class="fas fa-chevron-down fa-lg" id="icon-${companyId}"></i>
                </div>
            </div>
        </div>
        
        <div class="collapse company-content" id="collapse-${companyId}">
            <div class="p-3">
                <div class="mb-3">
                    <div class="input-group" style="max-width: 400px;">
                        <span class="input-group-text">
                            <i class="fas fa-search"></i>
                        </span>
                        <input type="text" class="form-control" placeholder="${t('search_within_company').replace('{company}', company)}" 
                               onkeyup="searchWithinGroup('${company}', this.value)">
                        <button class="btn btn-outline-secondary" onclick="clearGroupFilters('${company}')">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
                
                <div class="table-responsive">
                    <table class="table table-striped table-hover align-middle mb-0">
                        <thead>
                            <tr>
                                ${columns
                                  .map(
                                    (col) => `
                                    <th style="min-width: ${getColumnWidth(
                                      col
                                    )};">
                                        ${formatColumnName(col)}
                                    </th>
                                `
                                  )
                                  .join("")}
                            </tr>
                            <tr class="filter-row">
                                ${columns
                                  .map(
                                    (col) => `
                                    <th>
                                        ${createColumnFilter(col, company)}
                                    </th>
                                `
                                  )
                                  .join("")}
                            </tr>
                        </thead>
                        <tbody id="table-body-${company}">
                            ${renderTableRows(
                              data.slice(0, recordsPerPage),
                              columns,
                              company
                            )}
                        </tbody>
                    </table>
                </div>
                
                <div class="pagination-container d-flex justify-content-between align-items-center mt-3 p-3 bg-light rounded">
                    <div class="d-flex align-items-center gap-3">
                        <span class="text-muted">
                            Showing <span id="showing-${company}" class="fw-bold text-primary">1-${Math.min(
    recordsPerPage,
    data.length
  )}</span> 
                            of <span id="filtered-count-${company}" class="fw-bold">${
    data.length
  }</span> records
                        </span>
                        <div class="d-flex align-items-center gap-2">
                            <label for="pageSize-${company}" class="form-label mb-0 text-muted">Show:</label>
                            <select id="pageSize-${company}" class="form-select form-select-sm" style="width: auto;" onchange="changePageSize('${company}', this.value)">
                                <option value="5">5</option>
                                <option value="10" selected>10</option>
                                <option value="25">25</option>
                                <option value="50">50</option>
                                <option value="100">100</option>
                            </select>
                        </div>
                    </div>
                    <div>
                        <nav>
                            <ul class="pagination pagination-sm mb-0" id="pagination-${company}">
                                ${createPagination(company, data.length, 1)}
                            </ul>
                        </nav>
                    </div>
                </div>
            </div>
        </div>
    `;

  return groupDiv;
}

// Get column width
function getColumnWidth(columnName) {
  switch (columnName.toLowerCase()) {
    case "notes":
      return "250px";
    case "confirmed":
      return "100px";
    case "company":
      return "150px";
    case "createddate":
      return "120px";
    default:
      return "120px";
  }
}

// Format column name
function formatColumnName(columnName) {
  if (!columnName || typeof columnName !== 'string') {
    return String(columnName || 'Unknown');
  }
  
  // Only translate the Notes column
  if (columnName.toLowerCase() === 'notes') {
    return t('notes_column');
  }
  
  // For all other columns, just format them properly without translation
  return (
    columnName.charAt(0).toUpperCase() +
    columnName.slice(1).replace(/([A-Z])/g, " $1")
  );
}

// Create column filter
function createColumnFilter(columnName, company) {
  const filterId = `filter-${company}-${columnName}`;

  if (columnName.toLowerCase() === "confirmed") {
    return `
            <select class="filter-input active-filter" id="${filterId}" onchange="filterByColumn('${company}', '${columnName}', this.value)">
                <option value="">${t('all')}</option>
                <option value="true">${t('yes')}</option>
                <option value="false" selected>${t('no')}</option>
            </select>
        `;
  } else {
    return `
            <input type="text" class="filter-input" id="${filterId}" 
                   placeholder="${t('filter_placeholder').replace('{column}', formatColumnName(columnName))}" 
                   onkeyup="debounce(() => filterByColumn('${company}', '${columnName}', this.value), 300)()">
        `;
  }
}

// Render table rows
function renderTableRows(data, columns, company) {
  return data
    .map(
      (record, index) => `
        <tr data-record-id="${record.id}">
            ${columns
              .map((col) => renderTableCell(record, col, company))
              .join("")}
        </tr>
    `
    )
    .join("");
}

// Render table cell
function renderTableCell(record, columnName, company) {
  const value = getRecordValue(record, columnName);
  
  // Check if this company has an assignee
  const groupId = `group-${company.replace(/\s+/g, "-")}`;
  const groupElement = document.getElementById(groupId);
  const hasAssignee = groupElement ? groupElement.getAttribute("data-has-assignee") === "true" : true;

  if (columnName.toLowerCase() === "confirmed") {
    // Explicitly handle 1, '1', true, 'true' as checked; 0, '0', false, 'false' as unchecked
    const isChecked = value === 1 || value === '1' || value === true || value === 'true';
    return `
            <td>
                <div class="form-check">
                    <input class="form-check-input" type="checkbox" 
                           ${isChecked ? "checked" : ""} 
                           ${!hasAssignee ? "disabled" : ""}
                           onchange="updateConfirmedStatus('${
                             record.id
                           }', this.checked)">
                </div>
            </td>
        `;
  } else if (columnName.toLowerCase() === "notes") {
    return `
            <td>
                <input type="text" class="form-control notes-input" 
                       value="${value || ""}" 
                       ${!hasAssignee ? "disabled" : ""}
                       onchange="updateNotes('${record.id}', this.value)"
                       placeholder="${t('add_notes_placeholder')}">
            </td>
        `;
  } else if (columnName.toLowerCase() === "createddate") {
    return `<td>${formatDate(value)}</td>`;
  } else {
    return `<td>${value || ""}</td>`;
  }
}

// Get record value
function getRecordValue(record, columnName) {
  // First check additionalProperties
  if (record.additionalProperties && record.additionalProperties.hasOwnProperty(columnName)) {
    return record.additionalProperties[columnName];
  }

  // Fallback to checking main properties (case-insensitive)
  const lowerCol = columnName.toLowerCase();
  if (record.hasOwnProperty(lowerCol)) {
    return record[lowerCol];
  }

  // Check with original casing
  if (record.hasOwnProperty(columnName)) {
    return record[columnName];
  }

  return "";
}

// Format date to show both date and time
function formatDate(dateString) {
  if (!dateString) return "";
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  } catch {
    return dateString;
  }
}

// Create pagination
function createPagination(company, totalRecords, currentPage) {
  const pageSize = getPageSize(company);
  const totalPages = Math.ceil(totalRecords / pageSize);
  if (totalPages <= 1) return "";

  let pagination = "";

  // First page button
  pagination += `
        <li class="page-item ${currentPage === 1 ? "disabled" : ""}">
            <a class="page-link" href="#" onclick="changePage('${company}', 1)" title="First page">
                <i class="fas fa-angle-double-left"></i>
            </a>
        </li>
    `;

  // Previous button
  pagination += `
        <li class="page-item ${currentPage === 1 ? "disabled" : ""}">
            <a class="page-link" href="#" onclick="changePage('${company}', ${
    currentPage - 1
  })" title="Previous page">
                <i class="fas fa-chevron-left"></i>
            </a>
        </li>
    `;

  // Page numbers with smart display
  const maxVisiblePages = 5;
  let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
  let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

  // Adjust start page if we're near the end
  if (endPage - startPage + 1 < maxVisiblePages) {
    startPage = Math.max(1, endPage - maxVisiblePages + 1);
  }

  // Add ellipsis before if needed
  if (startPage > 1) {
    pagination += `
            <li class="page-item">
                <a class="page-link" href="#" onclick="changePage('${company}', 1)">1</a>
            </li>
        `;
    if (startPage > 2) {
      pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }
  }

  // Page numbers
  for (let i = startPage; i <= endPage; i++) {
    pagination += `
            <li class="page-item ${currentPage === i ? "active" : ""}">
                <a class="page-link" href="#" onclick="changePage('${company}', ${i})">${i}</a>
            </li>
        `;
  }

  // Add ellipsis after if needed
  if (endPage < totalPages) {
    if (endPage < totalPages - 1) {
      pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }
    pagination += `
            <li class="page-item">
                <a class="page-link" href="#" onclick="changePage('${company}', ${totalPages})">${totalPages}</a>
            </li>
        `;
  }

  // Next button
  pagination += `
        <li class="page-item ${currentPage === totalPages ? "disabled" : ""}">
            <a class="page-link" href="#" onclick="changePage('${company}', ${
    currentPage + 1
  })" title="Next page">
                <i class="fas fa-chevron-right"></i>
            </a>
        </li>
    `;

  // Last page button
  pagination += `
        <li class="page-item ${currentPage === totalPages ? "disabled" : ""}">
            <a class="page-link" href="#" onclick="changePage('${company}', ${totalPages})" title="Last page">
                <i class="fas fa-angle-double-right"></i>
            </a>
        </li>
    `;

  return pagination;
}

// Change page
function changePage(company, page) {
  const data = getFilteredData(company);
  const pageSize = getPageSize(company);
  const totalPages = Math.ceil(data.length / pageSize);

  if (page < 1 || page > totalPages) return;

  const startIndex = (page - 1) * pageSize;
  const endIndex = startIndex + pageSize;
  const pageData = data.slice(startIndex, endIndex);

  const columns = getColumnNames();
  const tableBody = document.getElementById(`table-body-${company}`);
  tableBody.innerHTML = renderTableRows(pageData, columns, company);

  // Update pagination
  const pagination = document.getElementById(`pagination-${company}`);
  pagination.innerHTML = createPagination(company, data.length, page);

  // Update showing count
  const showing = document.getElementById(`showing-${company}`);
  showing.textContent = `${startIndex + 1}-${Math.min(endIndex, data.length)}`;

  // Store current page for this company
  if (!window.companyPagination) window.companyPagination = {};
  window.companyPagination[company] = page;
}

// Get page size for a company
function getPageSize(company) {
  const pageSizeSelect = document.getElementById(`pageSize-${company}`);
  return pageSizeSelect ? parseInt(pageSizeSelect.value) : recordsPerPage;
}

// Change page size
function changePageSize(company, newSize) {
  // Reset to page 1 when changing page size
  const data = getFilteredData(company);
  const pageSize = parseInt(newSize);
  const columns = getColumnNames();

  // Update table content
  const pageData = data.slice(0, pageSize);
  const tableBody = document.getElementById(`table-body-${company}`);
  tableBody.innerHTML = renderTableRows(pageData, columns, company);

  // Update pagination
  const pagination = document.getElementById(`pagination-${company}`);
  pagination.innerHTML = createPagination(company, data.length, 1);

  // Update showing count
  const showing = document.getElementById(`showing-${company}`);
  showing.textContent = `1-${Math.min(pageSize, data.length)}`;

  // Reset current page for this company
  if (!window.companyPagination) window.companyPagination = {};
  window.companyPagination[company] = 1;
}

// Update confirmed status
function updateConfirmedStatus(recordId, isConfirmed) {
  const record = allData.find((r) => r.id === recordId);
  if (record) {
    // Store original value if this is the first change for this record
    if (!originalValues.has(recordId)) {
      originalValues.set(recordId, {
        confirmed: record.confirmed,
        notes: record.notes,
      });
    }

    record.confirmed = isConfirmed;
    // Also update additionalProperties to ensure consistency when rendering
    if (record.additionalProperties) {
      record.additionalProperties.confirmed = isConfirmed;
    }

    const original = originalValues.get(recordId);

    // Check if current values match original values
    const confirmedChanged = record.confirmed !== original.confirmed;
    const notesChanged = record.notes !== original.notes;

    if (confirmedChanged || notesChanged) {
      // There are actual changes, track them
      if (!pendingChanges.has(recordId)) {
        pendingChanges.set(recordId, { ...record });
      }
      pendingChanges.get(recordId).confirmed = isConfirmed;
      
      // Auto-update status to In Progress if assignee is set and status is Not Started
      if (isConfirmed) {
        const companyRecords = allData.filter(r => r.firmanr === record.firmanr);
        const hasAssignee = companyRecords[0]?.assigneeName || companyRecords[0]?.CompanyAssigneeName;
        const currentStatus = companyRecords[0]?.companyprocessedstatus || companyRecords[0]?.processedStatus || "Not Started";
        
        console.log(`Confirmed check - Firmanr: ${record.firmanr}, HasAssignee: ${hasAssignee}, Status: ${currentStatus}`);
        
        if (hasAssignee && hasAssignee !== "Unassigned" && currentStatus === "Not Started") {
          console.log(`Auto-updating status to In Progress for ${record.firmanr}`);
          // This will trigger its own autosave via addNewCompanyDetails
          handleEmployeeSelection(record.firmanr, null, "In Progress");
        }
      }
    } else {
      // No actual changes, remove from pending changes
      pendingChanges.delete(recordId);
      // If no changes at all, also remove from original values
      if (!confirmedChanged && !notesChanged) {
        originalValues.delete(recordId);
      }
    }

    // Check if all records for this company are now confirmed
    checkAndUpdateCompanyStatus(record.firmanr);

    // Update UI counts
    updateSummaryCounts();

    // Immediately save changes if there are any
    if (pendingChanges.size > 0) {
      saveAllChanges(true); // Immediate autosave
    }
  }
}

// Check if all records for a company are confirmed and update status accordingly
function checkAndUpdateCompanyStatus(company) {
  const companyRecords = allData.filter(r => r.firmanr === company);
  const allConfirmed = companyRecords.every(r => r.confirmed);
  
  if (allConfirmed && companyRecords.length > 0) {
    // All records are confirmed, update company status to Processed
    const currentStatus = companyRecords[0].companyprocessedstatus || companyRecords[0].processedStatus;
    if (currentStatus !== "Processed") {
      handleEmployeeSelection(company, null, "Processed");
    }
  }
}

// Update notes
function updateNotes(recordId, notes) {
  const record = allData.find((r) => r.id === recordId);
  if (record) {
    // Store original value if this is the first change for this record
    if (!originalValues.has(recordId)) {
      originalValues.set(recordId, {
        confirmed: record.confirmed,
        notes: record.notes,
      });
    }

    record.notes = notes;
    // Also update additionalProperties to ensure consistency when rendering
    if (record.additionalProperties) {
      record.additionalProperties.notes = notes;
    }

    const original = originalValues.get(recordId);

    // Check if current values match original values
    const confirmedChanged = record.confirmed !== original.confirmed;
    const notesChanged = record.notes !== original.notes;

    if (confirmedChanged || notesChanged) {
      // There are actual changes, track them
      if (!pendingChanges.has(recordId)) {
        pendingChanges.set(recordId, { ...record });
      }
      pendingChanges.get(recordId).notes = notes;
      
      // Auto-update status to In Progress if assignee is set and status is Not Started
      if (notes && notes.trim() !== "") {
        const companyRecords = allData.filter(r => r.firmanr === record.firmanr);
        const hasAssignee = companyRecords[0]?.assigneeName || companyRecords[0]?.CompanyAssigneeName;
        const currentStatus = companyRecords[0]?.companyprocessedstatus || companyRecords[0]?.processedStatus || "Not Started";
        
        console.log(`Notes check - Firmanr: ${record.firmanr}, HasAssignee: ${hasAssignee}, Status: ${currentStatus}`);
        
        if (hasAssignee && hasAssignee !== "Unassigned" && currentStatus === "Not Started") {
          console.log(`Auto-updating status to In Progress for ${record.firmanr}`);
          // This will trigger its own autosave via addNewCompanyDetails
          handleEmployeeSelection(record.firmanr, null, "In Progress");
        }
      }
    } else {
      // No actual changes, remove from pending changes
      pendingChanges.delete(recordId);
      // If no changes at all, also remove from original values
      if (!confirmedChanged && !notesChanged) {
        originalValues.delete(recordId);
      }
    }

    // Update UI counts
    updateSummaryCounts();

    // Show toast only if there are changes
    showSaveToast();
  }
}

// Update ProcessedStatus for a specific record
// Show save toast
function showSaveToast() {
  const changeCount = pendingChanges.size;

  if (changeCount >= 1) {
    // Start or reset autosave timer
    if (autoSaveTimeout) {
      clearTimeout(autoSaveTimeout);
    }
    autoSaveTimeout = setTimeout(() => {
      console.log("Auto-saving changes...");
      saveAllChanges(true); // Pass true to indicate it's an autosave
    }, AUTO_SAVE_DELAY);
  } else {
    // No changes, clear autosave timer
    if (autoSaveTimeout) {
      clearTimeout(autoSaveTimeout);
      autoSaveTimeout = null;
    }
  }
}

// Save all changes
async function saveAllChanges(isAutoSave = false) {
  if (pendingChanges.size === 0) return;

  // Clear autosave timer when manually saving or autosaving
  if (autoSaveTimeout) {
    clearTimeout(autoSaveTimeout);
    autoSaveTimeout = null;
  }

  const toast = document.getElementById('autosaveToast');
  const message = document.getElementById('autosaveMessage');

  try {
    const changeCount = pendingChanges.size;
    const changes = Array.from(pendingChanges.values());
    
    const response = await fetch("/api/data/save", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ changes }),
    });

    if (!response.ok) throw new Error("Save failed");

    const result = await response.json();

    // Show success toast
    message.textContent = `Saved ${changeCount} row${changeCount > 1 ? 's' : ''}!`;
    toast.classList.remove('error');
    toast.classList.add('show');

    // Clear pending changes and original values
    pendingChanges.clear();
    originalValues.clear();
    
    // Hide toast after 2 seconds
    setTimeout(() => {
      toast.classList.remove('show');
    }, 2000);
  } catch (error) {
    console.error("Save error:", error);
    
    // Show error toast
    message.textContent = 'Save failed!';
    toast.classList.add('error');
    toast.classList.add('show');
    
    // Hide toast after 3 seconds
    setTimeout(() => {
      toast.classList.remove('show');
      toast.classList.remove('error');
    }, 3000);
  }
}

// Filter functions
function performGlobalSearch() {
  const searchTerm = document
    .getElementById("globalSearch")
    .value.toLowerCase();

  console.log("=== performGlobalSearch called ===");
  console.log("Search term:", searchTerm);

  if (!searchTerm) {
    console.log("No search term, resetting to filtered data based on dropdowns");
    isShowingSearchResults = false; // Reset flag
    // Reset to filtered data based on current dropdown selections
    const currentStatus = getCurrentStatusFilter();
    const currentAssignee = getCurrentAssigneeFilter();
    filterCompanyBasedOnStatusnAssignee(currentStatus, currentAssignee);
    return;
  }

  console.log("Filtering allData, total records:", allData.length);

  // Start with all data, then apply filters
  let dataToFilter = allData;
  
  // Apply status and assignee filters first if they exist
  const currentStatus = getCurrentStatusFilter();
  const currentAssignee = getCurrentAssigneeFilter();
  
  if (currentStatus || currentAssignee) {
    dataToFilter = allData.filter((record) => {
      const actualAssigneeName = record.CompanyAssigneeName || record.assigneeName;
      
      if (currentStatus && currentAssignee) {
        return record.processedStatus === currentStatus && actualAssigneeName === currentAssignee;
      } else if (currentStatus) {
        return record.processedStatus === currentStatus;
      } else if (currentAssignee) {
        return actualAssigneeName === currentAssignee;
      }
      return true;
    });
  }

  // Apply global search - ONLY show records that match the search term
  const filteredData = dataToFilter.filter((record) => {
    const matches = Object.values(record).some(
      (value) => value && value.toString().toLowerCase().includes(searchTerm)
    );
    if (matches) {
      console.log("Record matches search:", record.firmanr, "- found in:", 
        Object.entries(record).filter(([key, value]) => 
          value && value.toString().toLowerCase().includes(searchTerm)
        ).map(([key, value]) => `${key}: ${value}`)
      );
    }
    return matches;
  });

  console.log("Filtered data length:", filteredData.length);

  // Regroup and display - this will group by company but only show matching records
  if (filteredData.length > 0) {
    console.log("Regrouping filtered data");
    isShowingSearchResults = true; // Set flag to prevent applyGroupFilters
    groupedData = groupDataByCompany(filteredData);
    renderGroupedData();
    
    // Auto-expand all accordions when showing search results
    setTimeout(() => {
      expandAllGroups();
      console.log("Auto-expanded all groups for search results");
    }, 100);
  } else {
    console.log("performGlobalSearch: No filtered data, showing empty result");
    isShowingSearchResults = true; // Set flag even for empty results
    groupedData = {};
    renderGroupedData();
  }
}

// Helper functions to get current filter values
function getCurrentStatusFilter() {
  const statusFilter = document.getElementById("statusFilter");
  return statusFilter ? statusFilter.value : null;
}

function getCurrentAssigneeFilter() {
  const assigneeFilter = document.getElementById("assigneeFilter");
  return assigneeFilter && assigneeFilter.value !== ""
    ? assigneeFilter.value
    : null;
}

// perform Assignment or Status change
function filterCompanyBasedOnStatusnAssignee(processedStatus, assignee) {
  console.log("=== filterCompanyBasedOnStatusnAssignee called ===");
  console.log("processedStatus:", processedStatus);
  console.log("assignee:", assignee);
  console.log("allData length:", allData.length);

  // Check if global search is active
  const globalSearchTerm = document.getElementById("globalSearch").value.toLowerCase();
  console.log("Global search term:", globalSearchTerm);

  // Don't reset search results flag if global search is active
  if (!globalSearchTerm) {
    isShowingSearchResults = false; // Only reset flag when no global search
  }

  const filteredData = allData.filter((record) => {
    // Get the actual assignee name (prioritize CompanyAssigneeName over assigneeName)
    const actualAssigneeName = record.CompanyAssigneeName || record.assigneeName;
    
    // First apply status and assignee filters
    let matchesFilters = true;
    
    // If in case of both values provided Status and assignee
    if (processedStatus && assignee) {
      matchesFilters = record.processedStatus === processedStatus && actualAssigneeName === assignee;
    }
    // If in case of only status
    else if (processedStatus) {
      matchesFilters = record.processedStatus === processedStatus;
    }
    // If in case of only assignee
    else if (assignee) {
      matchesFilters = actualAssigneeName === assignee;
    }
    // If no status/assignee filters, all records match the filter criteria
    else {
      matchesFilters = true;
    }
    
    // If global search is active, also apply global search filter
    if (globalSearchTerm && matchesFilters) {
      const matchesSearch = Object.values(record).some(
        (value) => value && value.toString().toLowerCase().includes(globalSearchTerm)
      );
      return matchesSearch;
    }
    
    return matchesFilters;
  });
  
  console.log("Filtered data length:", filteredData.length);
  
  // Update the display with filtered data
  if (filteredData.length > 0) {
    console.log("Regrouping filtered data by company");
    groupedData = groupDataByCompany(filteredData);
    renderGroupedData();
  } else {
    console.log("filterCompanyBasedOnStatusnAssignee: No filtered data, showing empty result");
    // Show empty state instead of keeping old data
    groupedData = {};
    renderGroupedData();
  }
}

function filterByColumn(company, columnName, filterValue) {
  const input = document.getElementById(`filter-${company}-${columnName}`);

  if (filterValue) {
    input.classList.add("active-filter");
  } else {
    input.classList.remove("active-filter");
  }

  applyGroupFilters(company);
}

function searchWithinGroup(company, searchTerm) {
  applyGroupFilters(company, searchTerm);
}

function applyGroupFilters(company, groupSearch = "") {
  console.log(`=== applyGroupFilters START for company: "${company}" ===`);
  
  const originalData = allData.filter(
    (r) => (r.firmanr || "Unknown") === company
  );
  console.log(`Original data for company "${company}":`, originalData.length, "records");
  
  let filteredData = [...originalData];

  // Apply group search
  if (groupSearch) {
    console.log(`Applying group search: "${groupSearch}"`);
    filteredData = filteredData.filter((record) =>
      Object.values(record).some(
        (value) =>
          value &&
          value.toString().toLowerCase().includes(groupSearch.toLowerCase())
      )
    );
    console.log(`After group search, filtered data:`, filteredData.length, "records");
  }

  // Apply column filters
  const columns = getColumnNames();
  columns.forEach((col) => {
    const filterInput = document.getElementById(`filter-${company}-${col}`);
    if (filterInput && filterInput.value) {
      const filterValue = filterInput.value.toLowerCase();

      if (col.toLowerCase() === "confirmed") {
        const boolValue = filterValue === "true";
        filteredData = filteredData.filter(
          (record) => record.confirmed === boolValue
        );
      } else {
        filteredData = filteredData.filter((record) => {
          const value = getRecordValue(record, col);
          return value && value.toString().toLowerCase().includes(filterValue);
        });
      }
    }
  });

  // Update display
  const pageSize = getPageSize(company);
  const tableBody = document.getElementById(`table-body-${company}`);
  const columns_list = getColumnNames();

  if (tableBody) {
    tableBody.innerHTML = renderTableRows(
      filteredData.slice(0, pageSize),
      columns_list,
      company
    );
  }

  // Update pagination
  const pagination = document.getElementById(`pagination-${company}`);
  if (pagination) {
    pagination.innerHTML = createPagination(company, filteredData.length, 1);
  }

  // Update counts
  const filteredCount = document.getElementById(`filtered-count-${company}`);
  const showing = document.getElementById(`showing-${company}`);

  if (filteredCount) {
    filteredCount.textContent = filteredData.length;
  }
  if (showing) {
    showing.textContent = `1-${Math.min(pageSize, filteredData.length)}`;
  }

  // Reset current page for this company
  if (!window.companyPagination) window.companyPagination = {};
  window.companyPagination[company] = 1;
}

function getFilteredData(company) {
  const originalData = allData.filter(
    (r) => (r.firmanr || "Unknown") === company
  );
  let filteredData = [...originalData];

  // Apply all active filters
  const columns = getColumnNames();
  columns.forEach((col) => {
    const filterInput = document.getElementById(`filter-${company}-${col}`);
    if (filterInput && filterInput.value) {
      const filterValue = filterInput.value.toLowerCase();

      if (col.toLowerCase() === "confirmed") {
        const boolValue = filterValue === "true";
        filteredData = filteredData.filter(
          (record) => record.confirmed === boolValue
        );
      } else {
        filteredData = filteredData.filter((record) => {
          const value = getRecordValue(record, col);
          return value && value.toString().toLowerCase().includes(filterValue);
        });
      }
    }
  });

  return filteredData;
}

function clearGroupFilters(company) {
  const columns = getColumnNames();
  columns.forEach((col) => {
    const filterInput = document.getElementById(`filter-${company}-${col}`);
    if (filterInput) {
      filterInput.value = "";
      filterInput.classList.remove("active-filter");
    }
  });

  // Clear group search - updated selector for new structure
  const companyId = company.replace(/\s+/g, "-").toLowerCase();
  const groupSearchInput = document.querySelector(
    `#collapse-${companyId} input[placeholder*="Search within"]`
  );
  if (groupSearchInput) groupSearchInput.value = "";

  applyGroupFilters(company);
}

function clearAllFilters() {
  console.log("=== clearAllFilters called ===");
  
  isShowingSearchResults = false; // Reset flag when clearing filters
  
  // Clear global search
  const globalSearch = document.getElementById("globalSearch");
  if (globalSearch) globalSearch.value = "";
  
  // Clear status filter
  const statusFilter = document.getElementById("statusFilter");
  if (statusFilter) statusFilter.value = "";
  
  // Clear assignee filter  
  const assigneeFilter = document.getElementById("assigneeFilter");
  if (assigneeFilter) assigneeFilter.value = "";
  
  // Clear group-level filters
  Object.keys(groupedData).forEach((company) => {
    clearGroupFilters(company);
  });
  
  // Reset to show all data
  processAndDisplayData();
}

// Expand all company accordions
function expandAllGroups() {
  Object.keys(groupedData).forEach((company) => {
    const companyId = company.replace(/\s+/g, "-").toLowerCase();
    const collapseElement = document.getElementById(`collapse-${companyId}`);
    const iconElement = document.getElementById(`icon-${companyId}`);
    
    if (collapseElement && iconElement) {
      // Expand the accordion
      if (!collapseElement.classList.contains('show')) {
        collapseElement.classList.add('show');
        iconElement.classList.remove('fa-chevron-down');
        iconElement.classList.add('fa-chevron-up');
        console.log(`Expanded accordion for company: ${company}`);
      }
    }
  });
}

// Collapse all company accordions  
function collapseAllGroups() {
  Object.keys(groupedData).forEach((company) => {
    const companyId = company.replace(/\s+/g, "-").toLowerCase();
    const collapseElement = document.getElementById(`collapse-${companyId}`);
    const iconElement = document.getElementById(`icon-${companyId}`);
    
    if (collapseElement && iconElement) {
      // Collapse the accordion
      if (collapseElement.classList.contains('show')) {
        collapseElement.classList.remove('show');
        iconElement.classList.remove('fa-chevron-up');
        iconElement.classList.add('fa-chevron-down');
        console.log(`Collapsed accordion for company: ${company}`);
      }
    }
  });
}

// Toggle show/hide processed companies
function toggleShowProcessed() {
  const checkbox = document.getElementById('toggleProcessed');
  showProcessedCompanies = checkbox.checked;
  console.log(`Toggle processed companies: ${showProcessedCompanies}`);
  
  // Re-render the grouped data with the new filter
  renderGroupedData();
}

// Toggle individual company accordion
function toggleCompanyAccordion(companyId) {
  const collapseElement = document.getElementById(`collapse-${companyId}`);
  const iconElement = document.getElementById(`icon-${companyId}`);
  
  if (collapseElement && iconElement) {
    if (collapseElement.classList.contains('show')) {
      // Collapse
      collapseElement.classList.remove('show');
      iconElement.classList.remove('fa-chevron-up');
      iconElement.classList.add('fa-chevron-down');
    } else {
      // Expand
      collapseElement.classList.add('show');
      iconElement.classList.remove('fa-chevron-down');
      iconElement.classList.add('fa-chevron-up');
    }
  }
}

// Update summary counts
function updateSummaryCounts() {
  Object.keys(groupedData).forEach((company) => {
    const data = groupedData[company];
    const confirmedCount = data.filter((r) => r.confirmed).length;
    const notConfirmedCount = data.length - confirmedCount;
    // Processed rows = rows with Confirmed checkbox OR rows with Notes text
    const processedRowsCount = data.filter((r) => r.confirmed || (r.notes && r.notes.trim() !== "")).length;

    const totalElement = document.getElementById(`total-rows-${company}`);
    const processedElement = document.getElementById(`processed-rows-${company}`);

    if (totalElement) totalElement.textContent = data.length;
    if (processedElement) processedElement.textContent = processedRowsCount;
  });
}

// Handle employee selection
function handleEmployeeSelection(
  company,
  assignee = null,
  processedStatus = null
) {
  if (company) {
    const companyObj = { Company: company, Assignee: assignee, ProcessedStatus: processedStatus };
    addNewCompanyDetails(companyObj);
  }
}
// Add new company details
function addNewCompanyDetails(companyObj) {
  fetch("/api/data/companies", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(companyObj),
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.json();
    })
    .then((data) => {
      updateAllDataBasedOnCompanyUpdate(companyObj);
      
      // Immediately update the accordion status dropdown to reflect the new status
      if (companyObj.ProcessedStatus) {
        const companyId = companyObj.Company.replace(/\s+/g, "-").toLowerCase();
        const statusDropdown = document.getElementById(`status-select-${companyId}`);
        if (statusDropdown) {
          statusDropdown.value = companyObj.ProcessedStatus;
        }
      }
      
      // Trigger autosave immediately for assignee/status changes
      console.log("Company details updated, triggering immediate autosave...");
      saveAllChanges(true);
    })
    .catch((error) => {
      console.error("Error adding company assignee:", error);
      showError("Error adding company assignee", "error");
    });
}

// Update All Data based on the new company update
function updateAllDataBasedOnCompanyUpdate(companyObj) {
  allData
    .filter((record) => record.firmanr === companyObj.Company)
    .forEach((record) => {
      if (companyObj.ProcessedStatus) {
        record.companyprocessedstatus = companyObj.ProcessedStatus;
      } else if (companyObj.Assignee) {
        const assignee = employeesLists.find(
          (emp) => emp.employeeID === companyObj.Assignee
        );
        record.assigneeName = assignee ? assignee.fullName : null;
        record.CompanyAssigneeName = assignee ? assignee.fullName : null;
      }
    });
    
  // Update the group's data-has-assignee attribute if assignee changed
  if (companyObj.Assignee) {
    const groupId = `group-${companyObj.Company.replace(/\s+/g, "-")}`;
    const groupElement = document.getElementById(groupId);
    if (groupElement) {
      const assignee = employeesLists.find(emp => emp.employeeID === companyObj.Assignee);
      const hasAssignee = assignee && assignee.fullName !== "Unassigned";
      groupElement.setAttribute("data-has-assignee", hasAssignee);
      
      // Re-render this company group to enable/disable inputs
      renderGroupedData();
    }
  }
  
  // If status changed to Processed, hide the company if toggle is off
  if (companyObj.ProcessedStatus === "Processed" && !showProcessedCompanies) {
    renderGroupedData();
  }
}

// Admin functions
function toggleAdminView() {
  const modal = new bootstrap.Modal(document.getElementById("adminModal"));
  modal.show();
  // Skip login, directly show admin panel
  isAdmin = true;
  document.getElementById("adminLogin").style.display = "none";
  document.getElementById("adminPanel").style.display = "block";
  
  // Load all data
  loadAuditLogs();
  loadCsvHistory();
  loadEmployees();
}

async function adminLogin(event) {
  event.preventDefault();

  const username = document.getElementById("adminUsername").value;
  const password = document.getElementById("adminPassword").value;

  try {
    const response = await fetch("/api/admin/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ username, password }),
    });

    const result = await response.json();

    if (result.success) {
      isAdmin = true;
      document.getElementById("adminLogin").style.display = "none";
      document.getElementById("adminPanel").style.display = "block";
      loadAuditLogs();
    } else {
      showError("Invalid admin credentials");
    }
  } catch (error) {
    showError("Login failed");
  }
}

async function adminLogout() {
  try {
    await fetch("/api/admin/logout", { method: "POST" });
    isAdmin = false;
    document.getElementById("adminLogin").style.display = "block";
    document.getElementById("adminPanel").style.display = "none";
    document.getElementById("adminUsername").value = "";
    document.getElementById("adminPassword").value = "";
  } catch (error) {
    console.error("Logout error:", error);
  }
}

async function loadAuditLogs() {
  if (!isAdmin) return;

  try {
    const response = await fetch("/api/admin/audit-logs");
    if (!response.ok) return;

    const logs = await response.json();
    const tableBody = document.getElementById("auditLogsTable");

    tableBody.innerHTML = logs
      .map(
        (log) => `
            <tr>
                <td>${formatDate(log.timestamp)}</td>
                <td><span class="badge bg-info">${log.action}</span></td>
                <td>${log.user}</td>
                <td><code>${log.recordId}</code></td>
                <td class="text-truncate" style="max-width: 200px;" title="${
                  log.changes
                }">
                    ${log.changes}
                </td>
            </tr>
        `
      )
      .join("");
  } catch (error) {
    console.error("Error loading audit logs:", error);
  }
}

async function searchAuditLogs() {
  if (!isAdmin) return;

  const searchTerm = document.getElementById("auditSearch").value;

  try {
    const response = await fetch(
      `/api/admin/audit-logs?searchTerm=${encodeURIComponent(searchTerm)}`
    );
    if (!response.ok) return;

    const logs = await response.json();
    const tableBody = document.getElementById("auditLogsTable");

    tableBody.innerHTML = logs
      .map(
        (log) => `
            <tr>
                <td>${formatDate(log.timestamp)}</td>
                <td><span class="badge bg-info">${log.action}</span></td>
                <td>${log.user}</td>
                <td><code>${log.recordId}</code></td>
                <td class="text-truncate" style="max-width: 200px;" title="${
                  log.changes
                }">
                    ${log.changes}
                </td>
            </tr>
        `
      )
      .join("");
  } catch (error) {
    console.error("Error searching audit logs:", error);
  }
}

// CSV Processing History functions
async function loadCsvHistory() {
  try {
    const response = await fetch("/api/admin/csv-history");
    if (!response.ok) return;

    const history = await response.json();
    const tableBody = document.getElementById("csvHistoryTable");

    tableBody.innerHTML = history.map(item => {
      const statusClass = getStatusClass(item.status);
      const statusBadge = `<span class="badge ${statusClass}">${item.status}</span>`;
      
      return `
        <tr>
          <td>${item.fileName}</td>
          <td>${item.timeBlock}</td>
          <td>${formatDate(item.processedDate)}</td>
          <td>${statusBadge}</td>
          <td><span class="badge bg-success">${item.recordsProcessed}</span></td>
          <td><span class="badge bg-${item.recordsSkipped > 0 ? 'warning' : 'secondary'}">${item.recordsSkipped}</span></td>
          <td>
            <button class="btn btn-sm btn-info" onclick="viewProcessingLogs('${item.fileName}', '${item.timeBlock}')">
              <i class="fas fa-eye"></i> View Logs
            </button>
          </td>
        </tr>
      `;
    }).join("");
  } catch (error) {
    console.error("Error loading CSV history:", error);
  }
}

function getStatusClass(status) {
  switch (status?.toLowerCase()) {
    case 'completed': case 'success': return 'bg-success';
    case 'failed': case 'error': return 'bg-danger';
    case 'processing': return 'bg-primary';
    case 'pending': return 'bg-warning';
    default: return 'bg-secondary';
  }
}

// Filter CSV History
function filterCsvHistory() {
  const filter = document.getElementById("csvHistoryFilter").value.toLowerCase();
  const table = document.getElementById("csvHistoryTable");
  const rows = table.getElementsByTagName("tr");

  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    const text = row.textContent || row.innerText;
    
    if (text.toLowerCase().indexOf(filter) > -1) {
      row.style.display = "";
    } else {
      row.style.display = "none";
    }
  }
}

function clearCsvHistoryFilter() {
  document.getElementById("csvHistoryFilter").value = "";
  filterCsvHistory();
}

// Processing Logs Modal functions
async function viewProcessingLogs(fileName, timeBlock) {
  try {
    const response = await fetch(`/api/admin/processing-logs?fileName=${encodeURIComponent(fileName)}&timeBlock=${encodeURIComponent(timeBlock)}`);
    if (!response.ok) return;

    const logs = await response.json();
    const tableBody = document.getElementById("processingLogsTable");
    document.getElementById("logFileName").textContent = `${fileName} (${timeBlock})`;

    tableBody.innerHTML = logs.map(log => {
      const levelClass = getLogLevelClass(log.logLevel);
      const levelBadge = `<span class="badge ${levelClass}">${log.logLevel}</span>`;
      
      return `
        <tr>
          <td>${formatDate(log.timestamp)}</td>
          <td>${levelBadge}</td>
          <td>${log.source}</td>
          <td>${log.message}</td>
          <td>${log.exception || ''}</td>
        </tr>
      `;
    }).join("");

    const modal = new bootstrap.Modal(document.getElementById("processingLogsModal"));
    modal.show();
  } catch (error) {
    console.error("Error loading processing logs:", error);
  }
}

function getLogLevelClass(level) {
  switch (level?.toLowerCase()) {
    case 'error': return 'bg-danger';
    case 'warning': return 'bg-warning';
    case 'info': return 'bg-info';
    case 'success': return 'bg-success';
    default: return 'bg-secondary';
  }
}

// Employee Management functions
async function loadEmployees() {
  try {
    const response = await fetch("/api/data/employees");
    if (!response.ok) return;

    const employees = await response.json();
    const tableBody = document.getElementById("employeesTable");

    // Check which employees can be deleted
    const deleteChecks = await Promise.all(
      employees.map(async (emp) => {
        try {
          const res = await fetch(`/api/admin/employee/can-delete/${encodeURIComponent(emp.employeeID)}`);
          const data = await res.json();
          return { employeeID: emp.employeeID, canDelete: data.canDelete };
        } catch {
          return { employeeID: emp.employeeID, canDelete: false };
        }
      })
    );

    const deleteMap = {};
    deleteChecks.forEach(check => {
      deleteMap[check.employeeID] = check.canDelete;
    });

    tableBody.innerHTML = employees.map(emp => {
      const canDelete = deleteMap[emp.employeeID];
      const deleteButton = canDelete 
        ? `<button class="btn btn-sm btn-danger" onclick="deleteEmployee('${emp.guid}')">
            <i class="fas fa-trash"></i>
          </button>`
        : `<button class="btn btn-sm btn-secondary" disabled title="Cannot delete: has non-processed records">
            <i class="fas fa-trash"></i>
          </button>`;
      
      return `
        <tr>
          <td>${emp.employeeID}</td>
          <td>${emp.firstName}</td>
          <td>${emp.lastName}</td>
          <td><span class="badge bg-${emp.isAdmin ? 'primary' : 'secondary'}">${emp.isAdmin ? 'Yes' : 'No'}</span></td>
          <td><span class="badge bg-${emp.isActive ? 'success' : 'danger'}">${emp.isActive ? 'Active' : 'Inactive'}</span></td>
          <td>
            <button class="btn btn-sm btn-warning" onclick='editEmployee(${JSON.stringify(emp)})'>
              <i class="fas fa-edit"></i>
            </button>
            ${deleteButton}
          </td>
        </tr>
      `;
    }).join("");
  } catch (error) {
    console.error("Error loading employees:", error);
  }
}

async function showAddEmployeeModal() {
  document.getElementById("employeeModalTitle").textContent = "Add Employee";
  document.getElementById("employeeForm").reset();
  document.getElementById("employeeGuid").value = "";
  document.getElementById("isActive").checked = true;
  
  // Auto-fetch next employee ID
  try {
    const response = await fetch("/api/admin/employee/next-id");
    if (response.ok) {
      const data = await response.json();
      document.getElementById("employeeID").value = data.nextId;
      document.getElementById("employeeID").setAttribute("readonly", "readonly");
    }
  } catch (error) {
    console.error("Error fetching next employee ID:", error);
    document.getElementById("employeeID").value = "1001";
    document.getElementById("employeeID").setAttribute("readonly", "readonly");
  }
  
  const modal = new bootstrap.Modal(document.getElementById("employeeModal"));
  modal.show();
}

function editEmployee(employee) {
  document.getElementById("employeeModalTitle").textContent = "Edit Employee";
  document.getElementById("employeeGuid").value = employee.guid;
  document.getElementById("employeeID").value = employee.employeeID;
  document.getElementById("employeeID").setAttribute("readonly", "readonly");
  document.getElementById("firstName").value = employee.firstName;
  document.getElementById("lastName").value = employee.lastName;
  document.getElementById("isAdmin").checked = employee.isAdmin;
  document.getElementById("isActive").checked = employee.isActive;
  
  const modal = new bootstrap.Modal(document.getElementById("employeeModal"));
  modal.show();
}

async function saveEmployee() {
  const guid = document.getElementById("employeeGuid").value;
  const employeeIDValue = document.getElementById("employeeID").value;
  
  const employee = {
    guid: guid || "00000000-0000-0000-0000-000000000000",
    employeeID: employeeIDValue || "",
    firstName: document.getElementById("firstName").value,
    lastName: document.getElementById("lastName").value,
    isAdmin: document.getElementById("isAdmin").checked,
    isActive: document.getElementById("isActive").checked
  };

  try {
    const url = "/api/admin/employee";
    const method = guid ? "PUT" : "POST";
    
    const response = await fetch(url, {
      method: method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(employee)
    });

    if (response.ok) {
      bootstrap.Modal.getInstance(document.getElementById("employeeModal")).hide();
      loadEmployees();
      await refreshEmployeeDropdowns(); // Refresh all employee dropdowns
      showToast(guid ? "Employee updated successfully!" : "Employee added successfully!", "success");
    } else {
      showToast("Failed to save employee", "error");
    }
  } catch (error) {
    console.error("Error saving employee:", error);
    showToast("Error saving employee", "error");
  }
}

// Refresh employee dropdowns without reloading the entire page
async function refreshEmployeeDropdowns() {
  try {
    // Reload the employees list
    const employeeResponse = await fetch("/api/data/employees");
    if (!employeeResponse.ok) throw new Error("Failed to fetch employees");
    
    employeesLists = await employeeResponse.json();
    
    // Update the global filter dropdown
    const assigneeFilter = document.getElementById("assigneeFilter");
    if (assigneeFilter) {
      const currentValue = assigneeFilter.value;
      assigneeFilter.innerHTML = `
        <option value="" selected>${t('select_employee')}</option>
        ${employeesLists
          .map(emp => `<option value="${emp.fullName}">${emp.fullName}</option>`)
          .join("")}
      `;
      assigneeFilter.value = currentValue; // Restore selection
    }
    
    // Update all company-specific employee dropdowns
    const companySelects = document.querySelectorAll('[id^="employee-select-"]');
    companySelects.forEach(select => {
      const currentValue = select.value;
      const currentAssigneeName = select.options[select.selectedIndex]?.text || '';
      
      select.innerHTML = `
        <option value="">${t('select_employee')}</option>
        ${employeesLists
          .map(emp => `<option value="${emp.employeeID}" ${emp.employeeID === currentValue ? 'selected' : ''}>${emp.fullName}</option>`)
          .join("")}
      `;
    });
    
    console.log("Employee dropdowns refreshed successfully");
  } catch (error) {
    console.error("Error refreshing employee dropdowns:", error);
  }
}

async function deleteEmployee(guid) {
  if (!confirm("Are you sure you want to delete this employee?")) return;

  try {
    const response = await fetch(`/api/admin/employee/${guid}`, { method: "DELETE" });
    
    if (response.ok) {
      loadEmployees();
      showToast("Employee deleted successfully!", "success");
    } else {
      const error = await response.json();
      showToast(error.message || "Cannot delete employee with non-processed assigned records", "error");
    }
  } catch (error) {
    console.error("Error deleting employee:", error);
    showToast("Error deleting employee", "error");
  }
}

// New data notifications
async function checkForNewData() {
  try {
    const response = await fetch("/api/data/check-updates");
    if (!response.ok) return;

    const notification = await response.json();

    if (notification.hasNewData) {
      showNewDataBanner(notification.newRecordCount);
    }
  } catch (error) {
    console.error("Error checking for new data:", error);
  }
}

function showNewDataBanner(count) {
  const banner = document.getElementById("newDataBanner");
  const countSpan = document.getElementById("newDataCount");

  countSpan.textContent = `(${count} new record${count > 1 ? "s" : ""})`;
  banner.style.display = "block";
}

function hideNewDataBanner() {
  document.getElementById("newDataBanner").style.display = "none";
}

async function saveAndRefresh() {
  if (pendingChanges.size > 0) {
    await saveAllChanges();
    setTimeout(() => {
      window.location.reload();
    }, 1000);
  } else {
    window.location.reload();
  }
}

// Accordion functions
function expandAllGroups() {
  Object.keys(groupedData).forEach((company) => {
    const companyId = company.replace(/\s+/g, "-").toLowerCase();
    const collapseElement = document.getElementById(`collapse-${companyId}`);
    const icon = document.getElementById(`icon-${companyId}`);

    if (collapseElement && !collapseElement.classList.contains("show")) {
      collapseElement.classList.add("show");
      if (icon) {
        icon.classList.remove("fa-chevron-down");
        icon.classList.add("fa-chevron-up");
      }
    }
  });
}

function collapseAllGroups() {
  Object.keys(groupedData).forEach((company) => {
    const companyId = company.replace(/\s+/g, "-").toLowerCase();
    const collapseElement = document.getElementById(`collapse-${companyId}`);
    const icon = document.getElementById(`icon-${companyId}`);

    if (collapseElement && collapseElement.classList.contains("show")) {
      collapseElement.classList.remove("show");
      if (icon) {
        icon.classList.remove("fa-chevron-up");
        icon.classList.add("fa-chevron-down");
      }
    }
  });
}

// Utility functions
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

function showLoading(show) {
  const spinner = document.getElementById("loadingSpinner");
  const container = document.getElementById("dataContainer");

  if (show) {
    spinner.style.display = "block";
    container.style.display = "none";
  } else {
    spinner.style.display = "none";
    container.style.display = "block";
  }
}

function showError(message) {
  // Create a temporary error toast
  const errorToast = document.createElement("div");
  errorToast.className = "toast";
  errorToast.innerHTML = `
        <div class="toast-body bg-danger text-white">
            <i class="fas fa-exclamation-triangle me-2"></i>
            ${message}
        </div>
    `;

  document.querySelector(".toast-container").appendChild(errorToast);

  const toast = new bootstrap.Toast(errorToast);
  toast.show();

  setTimeout(() => {
    errorToast.remove();
  }, 5000);
}

// Custom accordion toggle function
function toggleCompanyAccordion(companyId) {
  const collapseElement = document.getElementById(`collapse-${companyId}`);
  const icon = document.getElementById(`icon-${companyId}`);

  if (!collapseElement) return;

  if (collapseElement.classList.contains("show")) {
    // Hide the accordion
    collapseElement.classList.remove("show");
    if (icon) {
      icon.classList.remove("fa-chevron-up");
      icon.classList.add("fa-chevron-down");
    }
  } else {
    // Show the accordion
    collapseElement.classList.add("show");
    if (icon) {
      icon.classList.remove("fa-chevron-down");
      icon.classList.add("fa-chevron-up");
    }
  }
}

// Column Visibility Functions
function initializeColumnVisibility() {
  // Get all unique columns from the data's AdditionalProperties
  if (allData.length > 0) {
    const sampleRecord = allData[0];
    if (sampleRecord.additionalProperties) {
      allDatabaseColumns = Object.keys(sampleRecord.additionalProperties);
      // Initialize visibility settings - show all columns except hidden ones
      allDatabaseColumns.forEach((col) => {
        const isHiddenColumn = hiddenColumns.includes(col.toLowerCase());
        columnVisibilitySettings[col] = !isHiddenColumn;
      });

      // Always show the essential columns at the beginning
      essentialColumns.forEach((col) => {
        const found = allDatabaseColumns.find(
          (dbCol) => dbCol.toLowerCase() === col.toLowerCase()
        );
        if (found) {
          columnVisibilitySettings[found] = true;
        }
      });
    }
  } else {
    // No data available for column initialization
  }
}

function showColumnVisibilityModal() {
  const modal = new bootstrap.Modal(
    document.getElementById("columnVisibilityModal")
  );
  populateColumnCheckboxes();
  modal.show();
}

function populateColumnCheckboxes() {
  const container = document.getElementById("columnCheckboxes");
  container.innerHTML = "";

  // Group columns: Hidden columns first, then data columns
  const dataColumns = allDatabaseColumns.filter(
    (col) => !hiddenColumns.includes(col.toLowerCase())
  );
  const hiddenCols = allDatabaseColumns.filter((col) =>
    hiddenColumns.includes(col.toLowerCase())
  );

  // Add hidden columns section
  if (hiddenCols.length > 0) {
    const hiddenSection = document.createElement("div");
    hiddenSection.className = "col-12 mb-3";
    hiddenSection.innerHTML = `
            <div class="column-visibility-section">
                <h6 class="text-muted mb-3">
                    <i class="fas fa-eye-slash me-1"></i> Hidden Columns (Hidden by Default)
                </h6>
                <div class="row" id="hiddenColumns">
                </div>
            </div>
        `;
    container.appendChild(hiddenSection);

    const hiddenContainer = hiddenSection.querySelector("#hiddenColumns");
    hiddenCols.forEach((col) => {
      const colDiv = createColumnCheckbox(col, true);
      hiddenContainer.appendChild(colDiv);
    });
  }

  // Add data columns section
  if (dataColumns.length > 0) {
    const dataSection = document.createElement("div");
    dataSection.className = "col-12";
    dataSection.innerHTML = `
            <div class="column-visibility-section">
                <h6 class="text-primary mb-3">
                    <i class="fas fa-table me-1"></i> Data Columns
                </h6>
                <div class="row" id="dataColumns">
                </div>
            </div>
        `;
    container.appendChild(dataSection);

    const dataContainer = dataSection.querySelector("#dataColumns");
    dataColumns.forEach((col) => {
      const colDiv = createColumnCheckbox(col, false);
      dataContainer.appendChild(colDiv);
    });
  }
}

function createColumnCheckbox(columnName, isHiddenColumn) {
  const colDiv = document.createElement("div");
  colDiv.className = "col-md-6 col-lg-4 mb-2";

  const isVisible = columnVisibilitySettings[columnName] || false;
  const displayName = formatColumnName(columnName);
  const isEssentialColumn = essentialColumns.some(
    (essential) => essential.toLowerCase() === columnName.toLowerCase()
  );
  const shouldDisable = isHiddenColumn || isEssentialColumn;

  colDiv.innerHTML = `
        <div class="form-check">
            <input class="form-check-input" type="checkbox" 
                   id="col-${columnName}" 
                   ${isVisible ? "checked" : ""} 
                   ${shouldDisable ? "disabled" : ""}>
            <label class="form-check-label ${
              shouldDisable ? "text-muted" : ""
            }" for="col-${columnName}">
                ${displayName}
                ${
                  isHiddenColumn
                    ? '<small class="text-muted">(Hidden)</small>'
                    : ""
                }
                ${
                  isEssentialColumn && !isHiddenColumn
                    ? '<small class="text-muted">(Essential)</small>'
                    : ""
                }
            </label>
        </div>
    `;

  return colDiv;
}

function selectAllColumns() {
  const checkboxes = document.querySelectorAll(
    '#columnCheckboxes input[type="checkbox"]:not([disabled])'
  );
  checkboxes.forEach((cb) => (cb.checked = true));
}

function deselectAllColumns() {
  const checkboxes = document.querySelectorAll(
    '#columnCheckboxes input[type="checkbox"]:not([disabled])'
  );
  checkboxes.forEach((cb) => (cb.checked = false));
}

function resetToDefault() {
  // Reset to default visibility settings
  allDatabaseColumns.forEach((col) => {
    const isSystemColumn = systemColumns.includes(col.toLowerCase());
    const isEssentialColumn = essentialColumns.some(
      (essential) => essential.toLowerCase() === col.toLowerCase()
    );
    const checkbox = document.getElementById(`col-${col}`);
    if (checkbox && !checkbox.disabled) {
      checkbox.checked = !isSystemColumn || isEssentialColumn;
    }
  });
}

function applyColumnVisibility() {
  // Update visibility settings based on checkbox states
  allDatabaseColumns.forEach((col) => {
    const checkbox = document.getElementById(`col-${col}`);
    if (checkbox) {
      columnVisibilitySettings[col] = checkbox.checked;
    }
  });

  // Re-render all tables with new column visibility
  renderGroupedData();

  // Close modal
  const modal = bootstrap.Modal.getInstance(
    document.getElementById("columnVisibilityModal")
  );
  modal.hide();

  // Show toast notification
  showToast("Column visibility updated successfully!", "success");
}

function getVisibleColumns() {
  const visibleColumns = allDatabaseColumns.filter((col) => columnVisibilitySettings[col]);
  
  return visibleColumns;
}

function showToast(message, type = "info") {
  // Create a temporary toast for notifications
  const toastContainer = document.querySelector(".toast-container");
  const toastId = "toast-" + Date.now();

  const toastHtml = `
        <div id="${toastId}" class="toast" role="alert">
            <div class="toast-body d-flex align-items-center">
                <i class="fas fa-${
                  type === "success"
                    ? "check-circle text-success"
                    : "info-circle text-info"
                } me-2"></i>
                ${message}
                <button type="button" class="btn-close ms-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

  toastContainer.insertAdjacentHTML("beforeend", toastHtml);
  const toast = new bootstrap.Toast(document.getElementById(toastId), {
    delay: 3000,
  });
  toast.show();

  // Remove toast element after it's hidden
  document
    .getElementById(toastId)
    .addEventListener("hidden.bs.toast", function () {
      this.remove();
    });
}

function populateCompanyHistoryTable(data, tableBody) {
  if (!data || data.length === 0) {
    tableBody.innerHTML = `
      <tr>
        <td colspan="5" class="text-center text-muted py-4">
          <i class="fas fa-info-circle me-2"></i>
          No history records found for this company.
        </td>
      </tr>
    `;
    return;
  }

  tableBody.innerHTML = data
    .map(
      (record) => `
    <tr>
      <td>${record.assigneeName || "N/A"}</td>
      <td>
        <span class="badge ${getProcessedStatusBadge(record.processedStatus)}">
          ${getTranslatedStatus(record.processedStatus) || t('unknown')}
        </span>
      </td>
      <td>${formatDate(record.created)}</td>
    </tr>
  `
    )
    .join("");
}

function getProcessedStatusBadge(status) {
  switch ((status || "").toLowerCase()) {
    case "completed":
    case "complete":
      return "bg-success";
    case "in progress":
    case "processing":
      return "bg-warning text-dark";
    case "failed":
    case "error":
      return "bg-danger";
    case "pending":
      return "bg-secondary";
    default:
      return "bg-light text-dark";
  }
}

// Grouping Configuration Functions
function showGroupingModal() {
  const modal = new bootstrap.Modal(document.getElementById('groupingModal'));
  
  // Populate available columns for grouping
  populateGroupingColumns();
  
  // Load current configuration
  loadCurrentGroupingConfig();
  
  modal.show();
}

function populateGroupingColumns() {
  const groupBySelect = document.getElementById('groupByColumn');
  const sortBySelect = document.getElementById('sortByColumn');
  
  // Clear existing options
  groupBySelect.innerHTML = '';
  sortBySelect.innerHTML = '';
  
  // Get available columns from configuration
  const availableColumns = dashboardConfig?.grouping?.availableGroupingColumns || 
                           ['company', 'contacted', 'createddate', 'modifieddate'];
  
  // Populate Group By dropdown
  availableColumns.forEach(column => {
    const option = document.createElement('option');
    option.value = column;
    option.textContent = formatColumnName(column);
    groupBySelect.appendChild(option);
  });
  
  // Populate Sort By dropdown (include all available columns)
  const allColumns = allDatabaseColumns.length > 0 ? allDatabaseColumns : availableColumns;
  allColumns.forEach(column => {
    const option = document.createElement('option');
    option.value = column;
    option.textContent = formatColumnName(column);
    sortBySelect.appendChild(option);
  });
}

function formatColumnName(column) {
  // Ensure column is a string
  if (!column || typeof column !== 'string') {
    return String(column || 'Unknown');
  }
  
  // Only translate the Notes column
  if (column.toLowerCase() === 'notes') {
    return t('notes_column');
  }
  
  // For all other columns, just format them properly without translation
  return column
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, str => str.toUpperCase())
    .trim();
}

function loadCurrentGroupingConfig() {
  const config = dashboardConfig?.grouping;
  
  if (config) {
    document.getElementById('enableGrouping').checked = config.enableGrouping !== false;
    document.getElementById('groupByColumn').value = config.groupByColumn || 'company';
    document.getElementById('sortByColumn').value = config.sortByColumn || 'createddate';
    document.getElementById('sortDirection').value = config.sortDirection || 'desc';
  }
  
  // Update preview
  updateGroupingPreview();
  
  // Show/hide grouping options based on enabled state
  toggleGroupingOptions();
}

function updateGroupingPreview() {
  const enabled = document.getElementById('enableGrouping').checked;
  const groupBy = document.getElementById('groupByColumn').value;
  const sortBy = document.getElementById('sortByColumn').value;
  const sortDirection = document.getElementById('sortDirection').value;
  
  const preview = document.getElementById('groupingPreview');
  
  if (!enabled) {
    preview.innerHTML = 'Data grouping is <strong>disabled</strong>. All data will be shown in a single table.';
  } else {
    const sortText = sortDirection === 'desc' ? 'descending' : 'ascending';
    preview.innerHTML = `Data will be grouped by <strong>${formatColumnName(groupBy)}</strong>, sorted by <strong>${formatColumnName(sortBy)}</strong> in <strong>${sortText}</strong> order.`;
  }
}

function toggleGroupingOptions() {
  const enabled = document.getElementById('enableGrouping').checked;
  const optionsDiv = document.getElementById('groupingOptions');
  
  if (enabled) {
    optionsDiv.style.display = 'block';
  } else {
    optionsDiv.style.display = 'none';
  }
  
  updateGroupingPreview();
}

// Add event listeners for real-time preview updates
document.addEventListener('DOMContentLoaded', function() {
  // Add event listeners after modal is in DOM
  setTimeout(() => {
    const enableCheckbox = document.getElementById('enableGrouping');
    const groupBySelect = document.getElementById('groupByColumn');
    const sortBySelect = document.getElementById('sortByColumn');
    const sortDirectionSelect = document.getElementById('sortDirection');
    
    if (enableCheckbox) {
      enableCheckbox.addEventListener('change', toggleGroupingOptions);
    }
    
    [groupBySelect, sortBySelect, sortDirectionSelect].forEach(element => {
      if (element) {
        element.addEventListener('change', updateGroupingPreview);
      }
    });
  }, 100);
});

async function applyGroupingConfiguration() {
  try {
    const newConfig = {
      enableGrouping: document.getElementById('enableGrouping').checked,
      groupByColumn: document.getElementById('groupByColumn').value,
      sortByColumn: document.getElementById('sortByColumn').value,
      sortDirection: document.getElementById('sortDirection').value,
      availableGroupingColumns: dashboardConfig?.grouping?.availableGroupingColumns || 
                                ['company', 'contacted', 'createddate', 'modifieddate']
    };
    
    // Update configuration on server
    const response = await fetch('/api/data/grouping-configuration', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(newConfig)
    });
    
    if (!response.ok) {
      throw new Error('Failed to update grouping configuration');
    }
    
    // Update local configuration
    if (!dashboardConfig) {
      dashboardConfig = {};
    }
    dashboardConfig.grouping = newConfig;
    
    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('groupingModal'));
    modal.hide();
    
    // Reprocess and display data with new configuration
    processAndDisplayData();
    
    // Show success message
    showToast("Grouping configuration updated successfully!", "success");
    
  } catch (error) {
    console.error('Error updating grouping configuration:', error);
    showToast("Failed to update grouping configuration. Please try again.", "error");
  }
}

// Company History Functions
async function showCompanyHistory(firmanr) {
  try {
    showLoading(true);
    const response = await fetch(`/api/data/companies/${encodeURIComponent(firmanr)}/history`);
    
    if (!response.ok) {
      throw new Error('Failed to fetch company history');
    }
    
    const history = await response.json();
    displayCompanyHistoryModal(firmanr, history);
  } catch (error) {
    console.error('Error fetching company history:', error);
    showError('Failed to load company history');
  } finally {
    showLoading(false);
  }
}

function displayCompanyHistoryModal(firmanr, history) {
  const modalHTML = `
    <div class="modal fade" id="companyHistoryModal" tabindex="-1" aria-labelledby="companyHistoryModalLabel" aria-hidden="true">
      <div class="modal-dialog modal-xl-custom">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title" id="companyHistoryModalLabel">
              <i class="fas fa-history"></i> ${t('history_for_company')}: ${firmanr}
            </h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            ${history.length === 0 ? 
              `<div class="alert alert-info">${t('no_history_records')}</div>` :
              `<div class="table-responsive">
                <table class="table table-striped table-hover">
                  <thead class="table-dark">
                    <tr>
                      <th>${t('date_time')}</th>
                      <th>${t('action')}</th>
                      <th>${t('field')}</th>
                      <th>${t('old_value')}</th>
                      <th>${t('new_value')}</th>
                      <th>${t('modified_by')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${history.map(h => `
                      <tr>
                        <td>${formatDate(h.timestamp)}</td>
                        <td>
                          <span class="badge ${getActionBadgeClass(h.action)}">
                            ${h.action}
                          </span>
                        </td>
                        <td><strong>${h.columnName}</strong></td>
                        <td>${h.oldValue || `<em class="text-muted">${t('none')}</em>`}</td>
                        <td>${h.newValue || `<em class="text-muted">${t('none')}</em>`}</td>
                        <td>${h.modifiedBy}</td>
                      </tr>
                    `).join('')}
                  </tbody>
                </table>
              </div>`
            }
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${t('close')}</button>
          </div>
        </div>
      </div>
    </div>
  `;

  // Remove existing modal if present
  const existingModal = document.getElementById('companyHistoryModal');
  if (existingModal) {
    existingModal.remove();
  }

  // Add new modal to body
  document.body.insertAdjacentHTML('beforeend', modalHTML);

  // Show the modal
  const modal = new bootstrap.Modal(document.getElementById('companyHistoryModal'));
  modal.show();
}

function getActionBadgeClass(action) {
  switch (action.toUpperCase()) {
    case 'CREATE': return 'bg-success';
    case 'UPDATE': return 'bg-warning text-dark';
    case 'DELETE': return 'bg-danger';
    default: return 'bg-secondary';
  }
}
