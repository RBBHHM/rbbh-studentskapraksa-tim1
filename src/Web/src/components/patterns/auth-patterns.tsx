import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Check, Lock, Minus, ShieldAlert } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Callout } from "@/components/ui/callout";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Display, Eyebrow, Heading, Text } from "@/components/ui/typography";
import { permissionMatrix, roles, type Role } from "@/lib/patterns/auth-patterns";

/**
 * Auth & RBAC screens: sign-in, forgot password, forbidden, and a role guard
 * illustration driven by a permission matrix. Presentation only — no request
 * ever leaves the browser — but every field and rule mirrors what a real
 * identity flow needs.
 */

/** Sign-in: brand panel plus an email/password form, the pairing every product uses. */
export function SignInPattern() {
  const { t } = useTranslation("patterns");

  return (
    <div className="grid w-full overflow-hidden rounded-sm border border-border-default lg:grid-cols-2">
      <div data-surface="brand" className="flex flex-col justify-between p-8">
        <Eyebrow>{t("authPattern.signIn.brandEyebrow")}</Eyebrow>
        <Display as="p" size="md" className="mt-6">
          {t("authPattern.signIn.brandHeadline")}
        </Display>
        <Text size="sm" className="mt-6 max-w-xs text-text-primary">
          {t("authPattern.signIn.brandBody")}
        </Text>
      </div>
      <div className="p-8">
        <Heading level={3} size={5}>
          {t("authPattern.signIn.title")}
        </Heading>
        <Text size="sm" tone="secondary" className="mt-1">
          {t("authPattern.signIn.subtitle")}
        </Text>
        <div className="mt-6 space-y-4">
          <div>
            <Label htmlFor="signin-email">{t("authPattern.signIn.emailLabel")}</Label>
            <Input id="signin-email" type="email" placeholder="h.mahmutovic@rbinternational.com" className="mt-1.5" />
          </div>
          <div>
            <Label htmlFor="signin-password">{t("authPattern.signIn.passwordLabel")}</Label>
            <Input id="signin-password" type="password" placeholder="••••••••••" className="mt-1.5" />
          </div>
          <div className="flex items-center justify-between">
            <Button variant="link" size="sm" className="px-0">
              {t("authPattern.signIn.forgotLink")}
            </Button>
          </div>
          <Button className="w-full">{t("authPattern.signIn.submit")}</Button>
        </div>
      </div>
    </div>
  );
}

/** Forgot password: a single field and a deliberately generic confirmation. */
export function ForgotPasswordPattern() {
  const { t } = useTranslation("patterns");

  return (
    <div className="mx-auto w-full max-w-sm rounded-sm border border-border-default p-8">
      <Lock aria-hidden="true" className="size-8 text-text-brand-accent" />
      <Heading level={3} size={5} className="mt-4">
        {t("authPattern.forgotPassword.title")}
      </Heading>
      <Text size="sm" tone="secondary" className="mt-1">
        {t("authPattern.forgotPassword.subtitle")}
      </Text>
      <div className="mt-6">
        <Label htmlFor="forgot-email">{t("authPattern.forgotPassword.emailLabel")}</Label>
        <Input id="forgot-email" type="email" placeholder="h.mahmutovic@rbinternational.com" className="mt-1.5" />
      </div>
      <Button className="mt-6 w-full">{t("authPattern.forgotPassword.submit")}</Button>
      <Text size="sm" tone="tertiary" className="mt-4">
        {t("authPattern.forgotPassword.note")}
      </Text>
    </div>
  );
}

/** 403: the screen a permission boundary reaches, not a broken link. */
export function ForbiddenPattern() {
  const { t } = useTranslation("patterns");

  return (
    <div className="mx-auto w-full max-w-md py-8 text-center">
      <ShieldAlert aria-hidden="true" className="mx-auto size-12 text-feedback-danger" />
      <p className="mt-4 font-mono text-2xs text-text-tertiary">{t("authPattern.forbidden.code")}</p>
      <Heading level={3} size={5} className="mt-2">
        {t("authPattern.forbidden.title")}
      </Heading>
      <Text size="sm" tone="secondary" className="mx-auto mt-2 max-w-sm">
        {t("authPattern.forbidden.body")}
      </Text>
      <div className="mt-6 flex justify-center gap-3">
        <Button variant="secondary">{t("authPattern.forbidden.requestAccess")}</Button>
        <Button variant="ghost">{t("authPattern.forbidden.goBack")}</Button>
      </div>
    </div>
  );
}

/** Role guard: switch role, watch the same matrix drive both the UI and a table. */
export function RoleGuardPattern() {
  const { t } = useTranslation("patterns");
  const [role, setRole] = useState<Role>("viewer");

  return (
    <div className="w-full">
      <Tabs value={role} onValueChange={(value) => setRole(value as Role)}>
        <TabsList aria-label={t("authPattern.roleGuard.switcherLabel")}>
          {roles.map((item) => (
            <TabsTrigger key={item} value={item}>
              {t(`authPattern.roleGuard.roles.${item}` as never) as string}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>

      <Callout tone="info" className="mt-4" title={t("authPattern.roleGuard.previewTitle")}>
        <p>{t(`authPattern.roleGuard.previews.${role}` as never) as string}</p>
      </Callout>

      <div className="mt-6 overflow-x-auto rounded-sm border border-border-default">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("authPattern.roleGuard.capability")}</TableHead>
              {roles.map((item) => (
                <TableHead key={item} className="text-center">
                  {t(`authPattern.roleGuard.roles.${item}` as never) as string}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {permissionMatrix.map((row) => (
              <TableRow key={row.capabilityKey} data-surface={row.grants[role] ? undefined : undefined}>
                <TableCell className="text-sm font-medium text-text-primary">
                  {t(`authPattern.roleGuard.capabilities.${row.capabilityKey}` as never) as string}
                </TableCell>
                {roles.map((item) => (
                  <TableCell key={item} className="text-center">
                    {row.grants[item] ? (
                      <Check
                        aria-label={t("authPattern.roleGuard.granted")}
                        className="mx-auto size-4 text-feedback-success"
                      />
                    ) : (
                      <Minus
                        aria-label={t("authPattern.roleGuard.notGranted")}
                        className="mx-auto size-4 text-text-tertiary"
                      />
                    )}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <Badge tone="brand" className="mt-4">
        {t("authPattern.roleGuard.currentRoleBadge", {
          role: t(`authPattern.roleGuard.roles.${role}` as never) as string,
        })}
      </Badge>
    </div>
  );
}
