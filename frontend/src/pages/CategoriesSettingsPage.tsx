import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Checkbox, Group, Select, TextInput } from '@mantine/core'
import { IconArrowLeft } from '@tabler/icons-react'
import { useTranslation } from 'react-i18next'
import { createCategory, listCategories, updateCategory } from '../lib/cozyApi'
import { CategoryType } from '../lib/types'

type Props = {
  token: string
  bookId: string
  onBack: () => void
}

/**
 * Settings sub-page for category management.
 */
export function CategoriesSettingsPage({ token, bookId, onBack }: Props) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [categoryName, setCategoryName] = useState('')
  const [categoryType, setCategoryType] = useState(String(CategoryType.Expense))
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)

  const categoriesQuery = useQuery({
    queryKey: ['categories', bookId],
    queryFn: () => listCategories(token, bookId)
  })

  const selectedCategory = categoriesQuery.data?.find((category) => category.id === selectedCategoryId)
  const categoryTypeOptions = [
    { label: t('typeExpense'), value: String(CategoryType.Expense) },
    { label: t('typeIncome'), value: String(CategoryType.Income) }
  ]

  const upsertCategory = useMutation({
    mutationFn: async () => {
      const payload = {
        nameEn: categoryName,
        nameZhHans: categoryName,
        type: Number(categoryType) as CategoryType,
        parentId: null,
        isActive: true
      }

      if (selectedCategory) {
        return updateCategory(token, bookId, selectedCategory.id, payload)
      }

      return createCategory(token, bookId, payload)
    },
    onSuccess: async () => {
      setCategoryName('')
      setSelectedCategoryId(null)
      await queryClient.invalidateQueries({ queryKey: ['categories', bookId] })
    }
  })

  const handleCategorySubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!categoryName.trim()) {
      return
    }

    upsertCategory.mutate()
  }

  const getCategoryTypeLabel = (value: CategoryType) =>
    value === CategoryType.Income ? t('typeIncome') : t('typeExpense')

  return (
    <section className="cl-page">
      <header className="cl-header">
        <div className="cl-header-inner">
          <Group>
            <Button variant="light" leftSection={<IconArrowLeft size={16} />} onClick={onBack}>
              {t('backButton')}
            </Button>
          </Group>
          <h1 className="cl-header-title">{t('categoriesTitle')}</h1>
          <p className="cl-header-subtitle">{t('categoriesSubtitle')}</p>
        </div>
      </header>

      <div className="cl-body">
        <div className="cl-card">
          <form onSubmit={handleCategorySubmit} className="cl-form-grid">
            <p className="cl-card-title">{selectedCategory ? t('editCategory') : t('createCategory')}</p>
            <TextInput value={categoryName} onChange={(event) => setCategoryName(event.currentTarget.value)} label={t('nameLabel')} required />
            <Select
              value={categoryType}
              onChange={(value) => setCategoryType(value ?? String(CategoryType.Expense))}
              data={categoryTypeOptions}
              label={t('typeLabel')}
            />
            <Checkbox label={t('activeLabel')} checked readOnly />
            <Button type="submit" loading={upsertCategory.isPending}>
              {selectedCategory ? t('saveCategory') : t('addCategory')}
            </Button>
          </form>
        </div>

        <div className="cl-card">
          <div className="cl-list">
            {categoriesQuery.data?.map((category) => (
              <div key={category.id} className="cl-list-row">
                <span className="cl-list-row-main">
                  <span className="cl-list-row-title">{i18n.language === 'zh' ? category.nameZhHans : category.nameEn}</span>
                  <span className="cl-list-row-meta">{getCategoryTypeLabel(category.type)}</span>
                </span>
                <Button
                  variant="light"
                  size="xs"
                  onClick={() => {
                    setSelectedCategoryId(category.id)
                    setCategoryName(i18n.language === 'zh' ? category.nameZhHans : category.nameEn)
                    setCategoryType(String(category.type))
                  }}
                >
                  {t('editButton')}
                </Button>
              </div>
            ))}
            {!categoriesQuery.data?.length ? <p className="cl-empty">{t('noCategories')}</p> : null}
          </div>
        </div>
      </div>
    </section>
  )
}
