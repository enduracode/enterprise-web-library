export async function registerCleanupPermissions(ctx) {
  await ctx.permission.hook("evaluate", async (event) => {
    if (event.effect !== "ask") return
    const agent = event.agent ?? (await ctx.session.get({ sessionID: event.sessionID })).agent
    if (agent !== "ewl-cleanup") return
    event.effect = "deny"
    event.message = "EWL cleanup uses existing grants only. Ask the primary agent to resolve this permission before reinvoking cleanup."
  })
}
