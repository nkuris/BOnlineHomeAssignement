// In dev mode (running in Vite), use the proxy route '/api'
// In production or when VITE_API_URL is explicitly set, use that value
const apiUrl = import.meta.env.VITE_API_URL;
const API_BASE_URL = (apiUrl && apiUrl.trim()) ? apiUrl : (import.meta.env.DEV ? '/api' : 'http://localhost:5000/api');

class ApiService {
  constructor(baseUrl = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  async request(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    const defaultOptions = {
      headers: {
        'Content-Type': 'application/json',
      },
      ...options,
    };

    try {
      const response = await fetch(url, defaultOptions);

      if (!response.ok) {
        const error = new Error(`API Error: ${response.status}`);
        error.status = response.status;
        throw error;
      }

      // Handle empty responses (204 No Content, etc.)
      if (response.status === 204 || response.headers.get('content-length') === '0') {
        return null;
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error(`API Request failed: ${endpoint}`, error);
      throw error;
    }
  }

  // Leads endpoints
  async getLeads() {
    return this.request('/leads');
  }

  async getLeadById(id) {
    return this.request(`/leads/${id}`);
  }

  async createLead(leadData) {
    return this.request('/leads', {
      method: 'POST',
      body: JSON.stringify(leadData),
    });
  }

  async updateLead(id, leadData) {
    return this.request(`/leads/${id}`, {
      method: 'PUT',
      body: JSON.stringify(leadData),
    });
  }

  async deleteLead(id) {
    return this.request(`/leads/${id}`, {
      method: 'DELETE',
    });
  }

  // Programs endpoints
  async getPrograms() {
    return this.request('/programs');
  }

  async getProgramById(id) {
    return this.request(`/programs/${id}`);
  }

  async createProgram(programData) {
    return this.request('/programs', {
      method: 'POST',
      body: JSON.stringify(programData),
    });
  }

  async updateProgram(id, programData) {
    return this.request(`/programs/${id}`, {
      method: 'PUT',
      body: JSON.stringify(programData),
    });
  }

  async deleteProgram(id) {
    return this.request(`/programs/${id}`, {
      method: 'DELETE',
    });
  }

  // Tenants endpoints
  async getTenants() {
    return this.request('/tenants');
  }

  async getTenantById(id) {
    return this.request(`/tenants/${id}`);
  }

  async createTenant(tenantData) {
    return this.request('/tenants', {
      method: 'POST',
      body: JSON.stringify(tenantData),
    });
  }

  async updateTenant(id, tenantData) {
    return this.request(`/tenants/${id}`, {
      method: 'PUT',
      body: JSON.stringify(tenantData),
    });
  }

  async deleteTenant(id) {
    return this.request(`/tenants/${id}`, {
      method: 'DELETE',
    });
  }
}

// Export singleton instance
export default new ApiService();
