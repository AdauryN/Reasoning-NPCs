# Reasoning-NPCs
Reasoning NPCs focuses on integrating Large Language Models into the behavior and decision-making processes of Non-Player Characters (NPCs) within the Unity engine. This system aims to create NPCs capable of context-aware reasoning and dynamic reactions to player actions .

# Overview
The core intent of this project is to provide a framework where an LLM receives real-time information from a Unity scene, understands the context, and generates responses that are translated into direct in-game actions
. This moves beyond simple dialogue-based AI to create enemies and characters that truly adapt their combat tactics and strategies based on the player's unique style
.
# Key Features
Dynamic Reasoning: NPCs can interpret complex contexts and reason about possible actions beyond predefined rules
.
Player Style Adaptation: The system utilizes machine learning to identify behavioral patterns, such as preferred combat styles or movement habits, allowing NPCs to personalize their challenges
.
Modular AI Framework: A technical system designed as a reusable plugin or framework for easy integration into various Unity projects
.
Contextual Grounding (RAG): The potential use of Retrieval-Augmented Generation (RAG) to provide NPCs with structured game data, such as environment context and gameplay rules, ensuring more reliable and realistic decisions
.
Local Execution: Leverages local deployment to reduce latency and maintain privacy, ensuring AI decisions are made in real-time without dependency on external APIs
.
# Technologies
Engine: Unity (Unity Hub, C#)
.
LLM Backend: Built using the LLM for Unity asset, utilizing LlamaLib, llama.cpp, and C++ libraries
.
Frameworks Considered: Semantic Kernel, Ollama, LMStudio, and LangChain
.
Search/Memory: Usearch library for fast similarity search within the RAG system
.
# Project Roadmap
The development follows a structured timeline including:
System Architecture Design: Establishing the modular integration layer
.
NPC Decision-Making System: Implementing the logic that translates LLM reasoning into scene actions
.
Player Behavior Analysis: Developing the machine learning component to track player tactics
.
Prototype Validation: A dedicated game prototype developed to showcase the system in a live gameplay environment
.
# License
This project utilizes components under the Apache 2.0 license
