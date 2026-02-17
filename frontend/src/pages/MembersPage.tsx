import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Button, Card, Group, Text, TextInput, Title } from '@mantine/core'
import { useTranslation } from 'react-i18next'
import { acceptInvite, createInvite } from '../lib/cozyApi'
import { saveSession } from '../lib/session'

type Props = {
  token: string
  bookId: string
  onBookJoined: (bookId: string) => void
}

/**
 * Handles invite generation and invite acceptance for book membership.
 */
export function MembersPage({ token, bookId, onBookJoined }: Props) {
  const { t } = useTranslation()
  const [inviteToken, setInviteToken] = useState('')

  const createInviteMutation = useMutation({
    mutationFn: () => createInvite(token, bookId)
  })

  const acceptInviteMutation = useMutation({
    mutationFn: () => acceptInvite(token, inviteToken),
    onSuccess: (result) => {
      saveSession({ token, bookId: result.bookId })
      onBookJoined(result.bookId)
      setInviteToken('')
    }
  })

  const handleAccept = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!inviteToken.trim()) {
      return
    }

    acceptInviteMutation.mutate()
  }

  return (
    <section className="page-panel">
      <div className="page-titlebar">
        <div>
          <Title order={2}>{t('membersTitle')}</Title>
          <p>{t('generateInvite')}</p>
        </div>
      </div>
      <div className="split-grid">
        <Card shadow="sm" radius="md" className="surface-card form-grid">
          <Text fw={600}>{t('generateInvite')}</Text>
          <Button onClick={() => createInviteMutation.mutate()} loading={createInviteMutation.isPending}>
            {t('createInviteLink')}
          </Button>
          {createInviteMutation.data && (
            <div className="rows-stack">
              <div className="list-row">
                <div className="list-row-main">
                  <span className="list-row-title">{t('inviteTokenLabel')}</span>
                  <span className="list-row-meta">{createInviteMutation.data.token}</span>
                </div>
              </div>
              <div className="list-row">
                <div className="list-row-main">
                  <span className="list-row-title">{t('inviteUrlLabel')}</span>
                  <span className="list-row-meta">{createInviteMutation.data.inviteUrl}</span>
                </div>
              </div>
            </div>
          )}
        </Card>

        <Card shadow="sm" radius="md" className="surface-card">
          <form onSubmit={handleAccept} className="form-grid">
            <Text fw={600}>{t('acceptInvite')}</Text>
            <TextInput value={inviteToken} onChange={(event) => setInviteToken(event.currentTarget.value)} label={t('inviteTokenInput')} required />
            <Group>
              <Button type="submit" loading={acceptInviteMutation.isPending}>
                {t('joinBook')}
              </Button>
            </Group>
          </form>
        </Card>
      </div>
    </section>
  )
}
