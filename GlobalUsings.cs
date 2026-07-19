// Resolve Object ambiguity between UnityEngine.Object and System.Object.
// Il2Cpp assemblies expose UnityEngine.Object in scope, and unqualified
// "Object" references throughout the codebase target UnityEngine.Object.
global using Object = UnityEngine.Object;
