import { Link } from 'react-router-dom'
import './PlaceholderPage.css'

type PlaceholderPageProps = {
  title: string
  description: string
}

export function PlaceholderPage({ title, description }: PlaceholderPageProps) {
  return (
    <main className="placeholder-page">
      <div className="placeholder-page__card">
        <h1 className="placeholder-page__title">{title}</h1>
        <p className="placeholder-page__desc">{description}</p>
        <Link to="/" className="placeholder-page__back">
          ← Back to home
        </Link>
      </div>
    </main>
  )
}
