// Global variables
let allData = [];
let groupedData = {};
let pendingChanges = new Map();
let originalValues = new Map(); // Track original values for change detection
let toastTimeout;
let isAdmin = false;
let currentPage = 1;
const recordsPerPage = 10;
let employeesLists = [];

// Column visibility settings
let columnVisibilitySettings = {};
let allDatabaseColumns = [];
let systemColumns = [];
let essentialColumns = [];
let dashboardConfig = null;
const statusOptions = ["Not Started", "In Progress", "Waiting", "Processed"];

// Toast management
const saveToast = new bootstrap.Toast(document.getElementById("saveToast"), {
  autohide: false,
});

// Initialize dashboard
document.addEventListener("DOMContentLoaded", function () {
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
  document
    .getElementById("globalSearch")
    .addEventListener("input", debounce(performGlobalSearch, 300));

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
    console.log("Fetching dashboard configuration...");
    systemColumns = dashboardConfig.systemColumns || [];
    essentialColumns = dashboardConfig.essentialColumns || [];
  } catch (error) {
    console.error("Error loading dashboard configuration:", error);
    // Fallback to default configuration
    systemColumns = [
      "id",
      "createddate",
      "modifieddate",
      "sourcefilename",
      "timeblock",
      "confirmed",
      "notes",
      "company",
    ];
    essentialColumns = ["confirmed", "notes", "company"];
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

    // Initialize column visibility settings
    initializeColumnVisibility();

    processAndDisplayData();
    filterCompanyBasedOnStatusnAssignee(statusOptions[0], null);
    showLoading(false);
  } catch (error) {
    console.error("Error initializing dashboard:", error);
    showError("Failed to load dashboard data. Please refresh the page.");
    showLoading(false);
  }
}

// Process and display data
function processAndDisplayData() {
  // Group data based on configuration
  if (dashboardConfig?.grouping?.enableGrouping) {
    groupedData = groupDataByColumn(allData, dashboardConfig.grouping);
  } else {
    // If grouping is disabled, treat all data as one group
    groupedData = { "All Records": allData };
  }

  // Render grouped data
  renderGroupedData();

  // Update summary counts
  updateSummaryCounts();
}

// Group data by configurable column
function groupDataByColumn(data, groupingConfig) {
  const grouped = {};
  const groupByColumn = groupingConfig.groupByColumn || 'company';
  const sortByColumn = groupingConfig.sortByColumn || 'createddate';
  const sortDirection = groupingConfig.sortDirection || 'asc';

  data.forEach((record) => {
    // Get the grouping value - check both direct properties and additional properties
    let groupValue = record[groupByColumn] || 
                     record.additionalProperties?.[groupByColumn] || 
                     record.additionalProperties?.[groupByColumn.toLowerCase()] ||
                     "Unknown";
    
    // Handle boolean values for grouping (like 'contacted')
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
      let aVal = a[sortByColumn] || a.additionalProperties?.[sortByColumn] || "";
      let bVal = b[sortByColumn] || b.additionalProperties?.[sortByColumn] || "";
      
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

  return grouped;
}

// Legacy function for backward compatibility
function groupDataByCompany(data) {
  const defaultGrouping = {
    groupByColumn: 'company',
    sortByColumn: 'createddate',
    sortDirection: 'desc'
  };
  return groupDataByColumn(data, defaultGrouping);
}

// Render grouped data
function renderGroupedData() {
  const container = document.getElementById("dataContainer");
  container.innerHTML = "";

  if (Object.keys(groupedData).length === 0) {
    container.innerHTML = `
      <div class="alert alert-warning text-center py-5 my-4" role="alert">
        <i class="fas fa-exclamation-triangle fa-3x mb-3 text-warning"></i>
        <h2 class="fw-bold mb-3">No Data Available</h2>
        <p class="lead mb-0">There are currently no records to display. Please check your filters or try refreshing the page.</p>
      </div>
    `;
    return;
  }

  // Get all column names dynamically
  const columns = getColumnNames();
  Object.keys(groupedData)
    .sort()
    .forEach((company) => {
      const groupData = groupedData[company];
      const groupElement = createCompanyGroup(company, groupData, columns);
      container.appendChild(groupElement);

      // Apply default confirmed filter after DOM is ready

      setTimeout(() => {
        applyGroupFilters(company);
      }, 0);
    });

  container.style.display = "block";
}

// Get column names dynamically
function getColumnNames() {
  if (allData.length === 0) return [];

  // If column visibility is not initialized, do it now
  if (Object.keys(columnVisibilitySettings).length === 0) {
    initializeColumnVisibility();
  }

  // Return only visible columns
  const visibleColumns = getVisibleColumns();

  // Put Confirmed and Notes at the beginning, then Company, then all other visible columns
  const specialColumns = essentialColumns.map((col) => col.toLowerCase());
  const remainingColumns = visibleColumns.filter(
    (col) => !specialColumns.includes(col.toLowerCase())
  );

  // Final column order: Essential columns first, then all other visible CSV data columns
  const finalOrder = [];

  // Add special columns in desired order if they are visible
  essentialColumns.forEach((colName) => {
    const found = visibleColumns.find(
      (col) => col.toLowerCase() === colName.toLowerCase()
    );
    if (found) finalOrder.push(found);
  });

  // Add remaining visible columns
  remainingColumns.forEach((col) => {
    finalOrder.push(col);
  });

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
                  ${statusOptions
                    .map(
                      (status) =>
                        `<option ${
                          status === statusOptions[0] ? "selected" : ""
                        } value="${status}">${status}</option>`
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
                  <option value=""> Select Assignee</option>
                  ${employeesLists
                    .map(
                      (emp) =>
                        `<option value="${emp.fullName}"> ${emp.fullName}</option>`
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

  const confirmedCount = data.filter((r) => r.confirmed).length;
  const notConfirmedCount = data.length - confirmedCount;
  const companyId = company.replace(/\s+/g, "-").toLowerCase();
  const assignee = data[0].assigneeName || "Unassigned";
  const processedStatus = data[0].processedStatus || "Not Started";
  groupDiv.innerHTML = `
        <div class="group-header" 
             onclick="toggleCompanyAccordion('${companyId}')"
             style="cursor: pointer;">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="d-flex gap-3">
                        <div style="min-width: 240px;">
                          <strong style="font-size: 1.1rem;">${company}</strong>
                        </div>
                        <span >
                          <span class="badge bg-light text-dark">
                              Total: 
                              <span id="total-${company}">${data.length}</span>
                          </span>
                        </span>
                        <span>
                          <span class="badge bg-success">
                              Confirmed: <span id="confirmed-${company}">${confirmedCount}</span>
                          </span>
                        </span>
                        <span>
                          <span class="badge bg-warning text-dark">
                              Not confirmed: <span id="not-confirmed-${company}">${notConfirmedCount}</span>
                          </span> 
                        </span>
                        <label>Assignee:</label>
                        <select id="employee-select-${companyId}" class="form-select form-select-sm bg-secondary text-white" style="width: auto; max-width: 200px;" onclick="event.stopPropagation();" onchange="handleEmployeeSelection('${company}', this.value, null)">
                            <option value="">Select Employee</option>
                            ${employeesLists
                              .map(
                                (emp) =>
                                  `<option  value="${emp.employeeID}" ${
                                    emp.fullName === assignee ? "selected" : ""
                                  }>${emp.fullName}</option>`
                              )
                              .join("")}
                        </select>
                        <label>Status:</label>
                        <select id="status-select-${companyId}" class="form-select form-select-sm bg-secondary text-white" style="width: auto; max-width: 200px;" onclick="event.stopPropagation();" onchange="handleEmployeeSelection('${company}', null, this.value)" >
                        <option value="">Select Status</option>    
                        ${statusOptions
                          .map(
                            (status) => ` 
                                <option value="${status}" ${
                              processedStatus === status ? "selected" : ""
                            }>${status}</option>
                            `
                          )
                          .join("")}  
                        </select>
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
                        <input type="text" class="form-control" placeholder="Search within ${company}..." 
                               onkeyup="searchWithinGroup('${company}', this.value)">
                        <button class="btn btn-outline-secondary" onclick="clearGroupFilters('${company}')">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                </div>
                
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead class="table-dark">
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
                            <tr class="filter-row table-light">
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
                <option value="">All</option>
                <option value="true">Yes</option>
                <option value="false" selected>No</option>
            </select>
        `;
  } else {
    return `
            <input type="text" class="filter-input" id="${filterId}" 
                   placeholder="Filter ${formatColumnName(columnName)}..." 
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

  if (columnName.toLowerCase() === "confirmed") {
    return `
            <td>
                <div class="form-check">
                    <input class="form-check-input" type="checkbox" 
                           ${value ? "checked" : ""} 
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
                       onchange="updateNotes('${record.id}', this.value)"
                       placeholder="Add notes...">
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
  const lowerCol = columnName.toLowerCase();

  // Check main properties first
  if (record.hasOwnProperty(lowerCol)) {
    return record[lowerCol];
  }

  // Check with original casing
  if (record.hasOwnProperty(columnName)) {
    return record[columnName];
  }

  // Check additional properties
  if (record.additionalProperties && record.additionalProperties[columnName]) {
    return record.additionalProperties[columnName];
  }

  return "";
}

// Format date
function formatDate(dateString) {
  if (!dateString) return "";
  try {
    return new Date(dateString).toLocaleDateString();
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
    } else {
      // No actual changes, remove from pending changes
      pendingChanges.delete(recordId);
      // If no changes at all, also remove from original values
      if (!contactedChanged && !notesChanged) {
        originalValues.delete(recordId);
      }
    }

    // Show toast only if there are changes
    showSaveToast();
  }
}

// Show save toast
function showSaveToast() {
  const changeCount = pendingChanges.size;

  if (changeCount >= 1) {
    const message = document.getElementById("toastMessage");
    message.textContent = `${changeCount} row${
      changeCount > 1 ? "s" : ""
    } updated, SAVE?`;

    // Reset timer
    if (toastTimeout) {
      clearTimeout(toastTimeout);
    }

    saveToast.show();
  } else {
    // No changes, hide the toast
    saveToast.hide();
  }
}

// Save all changes
async function saveAllChanges() {
  if (pendingChanges.size === 0) return;

  const saveButton = document.getElementById("saveButtonText");
  const originalText = saveButton.textContent;

  try {
    // Show loading state
    saveButton.innerHTML =
      '<i class="fas fa-spinner fa-spin me-1"></i> Saving...';

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

    // Show success
    const toast = document.getElementById("saveToast");
    toast.classList.add("success");

    const message = document.getElementById("toastMessage");
    message.innerHTML = `<i class="fas fa-check me-2"></i>${result.message}`;

    saveButton.textContent = "SAVED";

    // Clear pending changes and original values
    pendingChanges.clear();
    originalValues.clear();
    // Hide toast after success
    setTimeout(() => {
      saveToast.hide();
      toast.classList.remove("success");
      message.innerHTML = "";
      saveButton.textContent = originalText;
    }, 3000);
  } catch (error) {
    console.error("Save error:", error);
    showError("Failed to save changes. Please try again.");
    saveButton.textContent = originalText;
  }
}

// Filter functions
function performGlobalSearch() {
  const searchTerm = document
    .getElementById("globalSearch")
    .value.toLowerCase();

  if (!searchTerm) {
    // Reset all data
    processAndDisplayData();
    return;
  }

  // Filter all data
  const filteredData = allData.filter((record) => {
    return Object.values(record).some(
      (value) => value && value.toString().toLowerCase().includes(searchTerm)
    );
  });

  // Regroup and display
  groupedData = groupDataByCompany(filteredData);
  renderGroupedData();
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
  const filteredData = allData.filter((record) => {
    // If in case of both values provided Status and assignee
    if (processedStatus && assignee) {
      return (
        record.processedStatus === processedStatus &&
        record.assigneeName === assignee
      );
    }
    // If in case of only stuatus
    else if (processedStatus) {
      return record.processedStatus === processedStatus;
    }
    // If in case of only assignee
    else if (assignee) {
      return record.assigneeName === assignee;
    }
    // rest case
    else {
      return true;
    }
  });
  // Update the display with filtered data
  groupedData = groupDataByCompany(filteredData);
  renderGroupedData();
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
  const originalData = allData.filter(
    (r) => (r.company || "Unknown") === company
  );
  let filteredData = [...originalData];

  // Apply group search
  if (groupSearch) {
    filteredData = filteredData.filter((record) =>
      Object.values(record).some(
        (value) =>
          value &&
          value.toString().toLowerCase().includes(groupSearch.toLowerCase())
      )
    );
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
    (r) => (r.company || "Unknown") === company
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
  document.getElementById("globalSearch").value = "";
  Object.keys(groupedData).forEach((company) => {
    clearGroupFilters(company);
  });
  processAndDisplayData();
}

// Update summary counts
function updateSummaryCounts() {
  Object.keys(groupedData).forEach((company) => {
    const data = groupedData[company];
    const confirmedCount = data.filter((r) => r.confirmed).length;
    const notConfirmedCount = data.length - confirmedCount;

    const totalElement = document.getElementById(`total-${company}`);
    const confirmedElement = document.getElementById(`confirmed-${company}`);
    const notConfirmedElement = document.getElementById(
      `not-confirmed-${company}`
    );

    if (totalElement) totalElement.textContent = data.length;
    if (confirmedElement) confirmedElement.textContent = confirmedCount;
    if (notConfirmedElement)
      notConfirmedElement.textContent = notConfirmedCount;
  });
}

// Handle employee selection
function handleEmployeeSelection(
  company,
  assignee = null,
  processedStatus = null
) {
  if (company) {
    const companyObj = { company, assignee, processedStatus };
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
      showToast("added successfully!", "success");
    })
    .catch((error) => {
      showError("Error adding company assignee", "error");
    });
}

// Update All Data based on the new company update
function updateAllDataBasedOnCompanyUpdate(companyObj) {
  allData
    .filter((record) => record.company === companyObj.company)
    .forEach((record) => {
      if (companyObj.processedStatus) {
        record.processedStatus = companyObj.processedStatus;
      } else if (companyObj.assignee) {
        const assignee = employeesLists.find(
          (emp) => emp.employeeID === companyObj.assignee
        );
        record.assigneeName = assignee ? assignee.fullName : null;
      }
    });
}

// Admin functions
function toggleAdminView() {
  const modal = new bootstrap.Modal(document.getElementById("adminModal"));
  modal.show();
  loadAuditLogs();
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
  // Get all unique columns from the data
  if (allData.length > 0) {
    const sampleRecord = allData[0];
    allDatabaseColumns = Object.keys(sampleRecord);

    // Add additional properties columns if they exist
    if (sampleRecord.additionalProperties) {
      Object.keys(sampleRecord.additionalProperties).forEach((col) => {
        if (!allDatabaseColumns.includes(col)) {
          allDatabaseColumns.push(col);
        }
      });
    }

    // Initialize visibility settings - hide system columns by default
    allDatabaseColumns.forEach((col) => {
      const isSystemColumn = systemColumns.includes(col.toLowerCase());
      columnVisibilitySettings[col] = !isSystemColumn;
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

  // Group columns: System columns first, then data columns
  const dataColumns = allDatabaseColumns.filter(
    (col) => !systemColumns.includes(col.toLowerCase())
  );
  const systemCols = allDatabaseColumns.filter((col) =>
    systemColumns.includes(col.toLowerCase())
  );

  // Add system columns section
  if (systemCols.length > 0) {
    const systemSection = document.createElement("div");
    systemSection.className = "col-12 mb-3";
    systemSection.innerHTML = `
            <div class="column-visibility-section">
                <h6 class="text-muted mb-3">
                    <i class="fas fa-cog me-1"></i> System Columns (Hidden by Default)
                </h6>
                <div class="row" id="systemColumns">
                </div>
            </div>
        `;
    container.appendChild(systemSection);

    const systemContainer = systemSection.querySelector("#systemColumns");
    systemCols.forEach((col) => {
      const colDiv = createColumnCheckbox(col, true);
      systemContainer.appendChild(colDiv);
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

function createColumnCheckbox(columnName, isSystemColumn) {
  const colDiv = document.createElement("div");
  colDiv.className = "col-md-6 col-lg-4 mb-2";

  const isVisible = columnVisibilitySettings[columnName] || false;
  const displayName = formatColumnName(columnName);
  const isEssentialColumn = essentialColumns.some(
    (essential) => essential.toLowerCase() === columnName.toLowerCase()
  );
  const shouldDisable = isSystemColumn || isEssentialColumn;

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
                  isSystemColumn
                    ? '<small class="text-muted">(System)</small>'
                    : ""
                }
                ${
                  isEssentialColumn && !isSystemColumn
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
  return allDatabaseColumns.filter((col) => columnVisibilitySettings[col]);
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
          ${record.processedStatus || "Unknown"}
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
  
  // Convert camelCase or lowercase to Title Case
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
