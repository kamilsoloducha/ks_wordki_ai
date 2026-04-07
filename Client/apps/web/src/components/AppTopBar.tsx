import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import './AppTopBar.css'

export function AppTopBar() {
  const navigate = useNavigate()
  const { isAuthenticated, user, signOut } = useAuth()

  function handleSignOut() {
    signOut()
    navigate('/', { replace: true })
  }

  return (
    <header className="app-topbar" role="banner">
      <div className="app-topbar__inner">
        <Link to="/" className="app-topbar__logo" aria-label="Wordki home">
          <span className="app-topbar__logo-mark" aria-hidden>
            W
          </span>
          <span className="app-topbar__logo-text">Wordki</span>
        </Link>

        <nav className="app-topbar__actions" aria-label="Account">
          {isAuthenticated && user ? (
            <>
              <Link to="/dashboard" className="app-topbar__link">
                Dashboard
              </Link>
              <span
                className="app-topbar__user-email"
                title={user.email}
                aria-label={`Signed in as ${user.email}`}
              >
                {user.email}
              </span>
              <button
                type="button"
                className="app-topbar__btn app-topbar__btn--ghost"
                onClick={handleSignOut}
              >
                Sign out
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                className="app-topbar__btn app-topbar__btn--ghost"
                onClick={() => navigate('/login')}
              >
                Sign in
              </button>
              <button
                type="button"
                className="app-topbar__btn app-topbar__btn--primary"
                onClick={() => navigate('/register')}
              >
                Create account
              </button>
            </>
          )}
        </nav>
      </div>
    </header>
  )
}
