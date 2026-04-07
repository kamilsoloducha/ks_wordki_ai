import { useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { useUserCardGroups } from '../hooks/useUserCardGroups'
import { useUserWordCount } from '../hooks/useUserWordCount'
import { useWordsDueTodayCount } from '../hooks/useWordsDueTodayCount'
import './DashboardPage.css'
import './RegisterPage.css'

export function DashboardPage() {
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()
  const { groups, loading, error } = useUserCardGroups(user?.id)
  const {
    wordCount,
    loading: wordsLoading,
    error: wordsError,
  } = useUserWordCount(user?.id)
  const {
    dueTodayCount,
    loading: dueTodayLoading,
    error: dueTodayError,
  } = useWordsDueTodayCount(user?.id)

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login', { replace: true, state: { from: '/dashboard' } })
    }
  }, [isAuthenticated, navigate])

  const groupCount = loading ? null : groups.length
  const combinedError = error ?? wordsError ?? dueTodayError

  if (!isAuthenticated || !user) {
    return null
  }

  return (
    <main className="register-page">
      <div className="register-page__card">
        <h1 className="register-page__title">Dashboard</h1>
        <p className="register-page__lead">
          Overview of your Wordki library.
        </p>

        {combinedError && (
          <div className="register-page__banner" role="alert">
            {combinedError}
          </div>
        )}

        <div className="dashboard-stats">
          <Link
            to="/lesson-settings"
            className="dashboard-stat dashboard-stat--link dashboard-stat--due"
            aria-labelledby="dashboard-due-today-heading"
          >
            <h2
              id="dashboard-due-today-heading"
              className="dashboard-stat__label"
            >
              Due today
            </h2>
            <p className="dashboard-stat__value" aria-live="polite">
              {dueTodayLoading ? '…' : dueTodayCount ?? '—'}
            </p>
            {!dueTodayLoading &&
              dueTodayCount === 0 &&
              !combinedError && (
                <p className="dashboard-stat__hint">
                  No words scheduled for review today.
                </p>
              )}
          </Link>

          <Link
            to="/groups"
            className="dashboard-stat dashboard-stat--link"
            aria-labelledby="dashboard-groups-heading"
          >
            <h2
              id="dashboard-groups-heading"
              className="dashboard-stat__label"
            >
              Card groups
            </h2>
            <p className="dashboard-stat__value" aria-live="polite">
              {loading ? '…' : groupCount ?? '—'}
            </p>
            {!loading && groupCount === 0 && !combinedError && (
              <p className="dashboard-stat__hint">
                You have no groups yet, or your account is not linked to the
                cards module. Groups you create will show up here.
              </p>
            )}
          </Link>

          <Link
            to="/groups"
            className="dashboard-stat dashboard-stat--link"
            aria-labelledby="dashboard-words-heading"
          >
            <h2 id="dashboard-words-heading" className="dashboard-stat__label">
              Words in library
            </h2>
            <p className="dashboard-stat__value" aria-live="polite">
              {wordsLoading ? '…' : wordCount ?? '—'}
            </p>
            {!wordsLoading &&
              wordCount === 0 &&
              !combinedError && (
                <p className="dashboard-stat__hint">
                  Add cards inside your groups to build your vocabulary here.
                </p>
              )}
          </Link>
        </div>

        <Link to="/" className="register-page__back">
          ← Back to home
        </Link>
      </div>
    </main>
  )
}
