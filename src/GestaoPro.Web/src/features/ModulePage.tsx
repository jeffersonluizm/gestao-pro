import { useParams } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { moduleForSession } from '../auth/session'
import { ForbiddenPage } from '../pages/system/ForbiddenPage'
import { NotFoundPage } from '../pages/system/NotFoundPage'
import { AnnouncementsFeed } from './announcements/AnnouncementsFeed'
import { useLookups } from './lookups'
import { canManageModule, canSeeModule, moduleBySlug } from './moduleRoutes'
import { PaymentsPage } from './payments/PaymentsPage'
import { billFilters, FilteredPage, occurrenceFilters, petFilters } from './resources/FilteredPage'
import { ResourcePage } from './resources/ResourcePage'
import { VotesPage } from './votes/VotesPage'

// /painel/:slug — escolhe a tela do módulo conforme o papel da sessão.
export function ModulePage() {
  const { slug = '' } = useParams()
  const { session, canManage } = useAuth()
  const { lookups, refresh } = useLookups()
  const baseModule = moduleBySlug(slug)

  if (!baseModule) return <NotFoundPage embedded />
  if (!canSeeModule(baseModule, session, canManage)) return <ForbiddenPage />

  const manages = canManageModule(baseModule, session, canManage)
  const module = moduleForSession(baseModule, session, canManage)

  const page = (() => {
    switch (module.id) {
      case 'announcements':
        return manages
          ? <ResourcePage key={slug} module={module} canManage session={session} lookups={lookups} />
          : <AnnouncementsFeed />
      case 'units':
        return <FilteredPage key={slug} module={module} filters={null} canManage={manages} session={session} lookups={lookups} />
      case 'pets':
        return <FilteredPage key={slug} module={module} filters={manages ? petFilters : null} canManage={manages} session={session} lookups={lookups} />
      case 'occurrences':
        return <FilteredPage key={slug} module={module} filters={manages ? occurrenceFilters : null} canManage={manages} session={session} lookups={lookups} />
      case 'bills':
        return <FilteredPage key={slug} module={module} filters={manages ? billFilters : null} canManage={manages} session={session} lookups={lookups} />
      case 'payments':
        return <PaymentsPage key={slug} module={module} canManage={manages} session={session} lookups={lookups} />
      case 'votes':
        return <VotesPage key={slug} module={module} canManage={manages} session={session} lookups={lookups} />
      default:
        return (
          <ResourcePage
            key={slug}
            module={module}
            canManage={manages}
            session={session}
            lookups={lookups}
            onChanged={module.id === 'assembly' || module.id === 'visitors' || module.id === 'deliveries' ? refresh : undefined}
          />
        )
    }
  })()

  return (
    <>
      <header className="page-heading">
        <div>
          <h1>{module.title}</h1>
          <p>{module.description}</p>
        </div>
      </header>
      {page}
    </>
  )
}
