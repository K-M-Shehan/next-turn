import { useCallback, useEffect, useState } from 'react'

const ONBOARDING_KEY_PREFIX = 'nextturn:onboarding:v1:'

function buildStorageKey(scope: string): string {
  return `${ONBOARDING_KEY_PREFIX}${scope}`
}

export function useOnboardingTour(scope: string) {
  const [isOpen, setIsOpen] = useState(false)
  const storageKey = buildStorageKey(scope)

  useEffect(() => {
    const hasCompleted = window.localStorage.getItem(storageKey) === '1'
    if (!hasCompleted) {
      setIsOpen(true)
    }
  }, [storageKey])

  const completeTour = useCallback(() => {
    window.localStorage.setItem(storageKey, '1')
    setIsOpen(false)
  }, [storageKey])

  const restartTour = useCallback(() => {
    window.localStorage.removeItem(storageKey)
    setIsOpen(true)
  }, [storageKey])

  return {
    isOpen,
    completeTour,
    restartTour,
    storageKey,
  }
}
