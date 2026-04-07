import { BrowserRouter, Outlet, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { AppTopBar } from './components/AppTopBar'
import { DashboardPage } from './pages/DashboardPage'
import { GroupDetailPage } from './pages/GroupDetailPage'
import { LessonSettingsPage } from './pages/LessonSettingsPage'
import { LessonPage } from './pages/LessonPage'
import { GroupsPage } from './pages/GroupsPage'
import { ImportGroupPage } from './pages/ImportGroupPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import './App.css'

function AppShell() {
  return (
    <div className="app-shell">
      <AppTopBar />
      <Outlet />
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<AppShell />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/lesson-settings" element={<LessonSettingsPage />} />
            <Route path="/lesson" element={<LessonPage />} />
            <Route path="/groups/import" element={<ImportGroupPage />} />
            <Route path="/groups/:groupId" element={<GroupDetailPage />} />
            <Route path="/groups" element={<GroupsPage />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
