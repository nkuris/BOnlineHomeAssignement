import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import LeadsDashboard from './pages/LeadsDashboard';
import ProgramsDashboard from './pages/ProgramsDashboard';
import TenantsDashboard from './pages/TenantsDashboard';
import './App.css';

function App() {
  return (
    <Router>
      <Layout>
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/leads" element={<LeadsDashboard />} />
          <Route path="/programs" element={<ProgramsDashboard />} />
          <Route path="/tenants" element={<TenantsDashboard />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </Layout>
    </Router>
  );
}

export default App;